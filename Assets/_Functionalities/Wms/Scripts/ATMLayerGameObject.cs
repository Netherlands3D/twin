using Netherlands3D.Coordinates;
using Netherlands3D.Rendering;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    public class ATMLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData
    {
        public ATMTileDataLayer ATMProjectionLayer { get { return ATMTileDataLayers[GetZoomLayerIndex(zoomLevel)]; } }
        public bool TransparencyEnabled = true; //this gives the requesting url the extra param to set transparancy enabled by default       
        public int DefaultEnabledLayersMax = 5;  //in case the dataset is very large with many layers. lets topggle the layers after this count to not visible.
        public Vector2Int PreferredImageSize = Vector2Int.one * 512;
        private const float earthRadius = 6378.137f;
        private const double equatorialCircumference = 2 * Mathf.PI * earthRadius;
        private const double log2x = 0.30102999566d;
        public LayerPropertyData PropertyData => urlPropertyData;

        private ATMTileDataLayer[] ATMTileDataLayers;
        private ATMTileDataLayer currentDataLayer;
        private ATMDataController timeController;
        private ATMDataController.ATMDataHandler ATMDataHandler;

        private int zoomLevel = -1;
        private int lastZoomLevel = -1;
        private Vector2Int zoomBounds = Vector2Int.zero;

        protected LayerURLPropertyData urlPropertyData = new();

        private bool isParentLayer = false;
        
        protected override void Awake()
        {
            base.Awake();  
        }

        void Update()
        {
            if (!isParentLayer)
                return;

            zoomLevel = CalculateZoomLevel();          
           
            UpdateCurrentZoomLayer();
        }

        private void UpdateCurrentZoomLayer(bool force = false)
        {
            if(ATMTileDataLayers == null || !isParentLayer)
                return;

            if (zoomLevel != lastZoomLevel || force)
            {
                CartesianTiles.TileHandler handler = GetComponentInParent<CartesianTiles.TileHandler>();
               
                //get the current enabled zoom layer and update it with setvisibletilesdirty
                int index = GetZoomLayerIndex(zoomLevel);

                for (int i = 0; i < ATMTileDataLayers.Length; i++)
                {
                    ATMTileDataLayers[i].isEnabled = index == i;
                    if(i != index)
                        ATMTileDataLayers[i].CancelTiles();
                }



                //for (int i = 0; i < ATMTileDataLayers.Length; i++)
                //{
                //    ATMTileDataLayers[i].isEnabled = true;
                //}

                //if (handler.layers.Contains(currentDataLayer))
                //    handler.RemoveLayer(currentDataLayer);

                //for (int i = 0; i < ATMTileDataLayers.Length; i++)
                //{
                //    if (i != index)
                //    {
                //        handler.RemoveLayer(ATMTileDataLayers[i]);
                //        handler.AddLayer(ATMTileDataLayers[i]);
                //    }
                //}
                //if (!handler.layers.Contains(ATMTileDataLayers[index]))
                //    handler.AddLayer(ATMTileDataLayers[index]);
                
                ATMTileDataLayers[index].SetVisibleTilesDirty();

                //for (int i = 0; i < ATMTileDataLayers.Length; i++)
                //    if (i != index)
                //    {
                //        //disable each layer after the refresh loop or tilehandler will error on the cached tilesizes
                //        ATMTileDataLayers[i].isEnabled = false;
                //    }

                lastZoomLevel = zoomLevel;
            }
        }

        protected override void Start()
        {
            base.Start();
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);

            ATMLayerGameObject parent = transform.parent.GetComponent<ATMLayerGameObject>();
            if (parent != null)
            {
                //when instantiating as a sublayer this overrides the parent, so we need to disable this agian
                currentDataLayer = GetComponent<ATMTileDataLayer>();                
                currentDataLayer.isEnabled = parent.zoomLevel == currentDataLayer.ZoomLevel;
                return;
            }

            isParentLayer = true;

            if (timeController == null)
            {
                timeController = gameObject.AddComponent<ATMDataController>();
                ATMDataHandler = (a) => UpdateCurrentZoomLayer(true);
                timeController.ChangeYear += ATMDataHandler;
            }

            currentDataLayer = GetComponent<ATMTileDataLayer>();
            TextureProjectorBase projectorPrefab = currentDataLayer.ProjectorPrefab;
            currentDataLayer.SetZoomLevel(-1);
            //current.enabled = false;
            currentDataLayer.isEnabled = false;

            Destroy(GetComponent<WorldTransform>());
            Destroy(GetComponent<ChildWorldTransformShifter>());

            CartesianTiles.TileHandler handler = GetComponentInParent<CartesianTiles.TileHandler>();           

            zoomLevel = CalculateZoomLevel();
            zoomBounds = timeController.GetZoomBoundsAllYears();
            int length = zoomBounds.y - zoomBounds.x + 1;
            ATMTileDataLayers = new ATMTileDataLayer[length];
            for (int i = 0; i < ATMTileDataLayers.Length; i++)
            {
                GameObject zoomLayerObject = new GameObject((zoomBounds.x + i).ToString());
                zoomLayerObject.AddComponent<XyzTiles>();
                WorldTransform wt = zoomLayerObject.AddComponent<WorldTransform>();
                ChildWorldTransformShifter cts = zoomLayerObject.AddComponent<ChildWorldTransformShifter>();
                wt.SetShifter(cts);
                Destroy(zoomLayerObject.GetComponent<GameObjectWorldTransformShifter>());
                
                ATMTileDataLayers[i] = zoomLayerObject.AddComponent<ATMTileDataLayer>();
                
                ATMTileDataLayer zoomLayer = ATMTileDataLayers[i].GetComponent<ATMTileDataLayer>();
                zoomLayer.ProjectorPrefab = projectorPrefab;
                zoomLayer.SetDataController(timeController);                
                zoomLayer.SetZoomLevel(zoomBounds.x + i);
                zoomLayer.tileSize = timeController.GetTileSizeForZoomLevel(zoomLayer.ZoomLevel);
                //zoomLayer.enabled = true;
                //handler.AddLayer(zoomLayer);
                zoomLayerObject.AddComponent<ATMLayerGameObject>();
                ATMTileDataLayers[i].transform.SetParent(transform, false);
            }
            //yes afterwards or the tilehandler will clear the tilesizes for inactive layers
            int index = GetZoomLayerIndex(zoomLevel);
            for (int i = 0; i < ATMTileDataLayers.Length; i++)
            {
                //if (!handler.layers.Contains(ATMTileDataLayers[i]))
                //    handler.AddLayer(ATMTileDataLayers[i]);

                if (i != index)
                    ATMTileDataLayers[i].isEnabled = false;
            }

            //handler.RemoveLayer(currentDataLayer);

            UpdateCurrentZoomLayer(true);
            SetRenderOrder(LayerData.RootIndex);
        }

        public void SetZoomBounds(Vector2Int bounds)
        {
            zoomBounds = bounds;
        }

        private int GetZoomLayerIndex(int zoomLevel)
        {          
            return zoomLevel - zoomBounds.x;
        }

        //a higher order means rendering over lower indices
        public void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            ATMProjectionLayer.RenderIndex = -order;
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
        }

        public int CalculateZoomLevel()
        {
            Vector3 camPosition = Camera.main.transform.position;      
            var unityCoordinate = new Coordinate(
                  CoordinateSystem.Unity,
                  camPosition.x,
                  camPosition.y,
                  camPosition.z
              );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
            double latitude = coord.northing;
            double cosLatitude = Math.Cos(latitude * Mathf.Deg2Rad);
            double zoomLevel = Math.Log(equatorialCircumference * cosLatitude / camPosition.y) / log2x;
            var currentYearZoomBounds = timeController.GetZoomBounds();
            zoomLevel = Math.Clamp(zoomLevel, currentYearZoomBounds.minZoom, currentYearZoomBounds.maxZoom);
            return (int)zoomLevel;  
        }

        public override void DestroyLayerGameObject()
        {
            base.DestroyLayerGameObject();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
            if (timeController != null && ATMDataHandler != null)
                timeController.ChangeYear -= ATMDataHandler;
        }
    }
}