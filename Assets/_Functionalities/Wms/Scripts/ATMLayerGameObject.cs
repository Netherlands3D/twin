using Netherlands3D.Coordinates;
using Netherlands3D.Rendering;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.AI;

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
        private const float equatorialCircumference = 2 * Mathf.PI * earthRadius;
        private const float log2x = 0.30102999566f;
        public LayerPropertyData PropertyData => urlPropertyData;

        private static ATMTileDataLayer[] ATMTileDataLayers;
        private ATMDataController timeController;
        private ATMDataController.ATMDataHandler ATMDataHandler;

        private int zoomLevel = -1;
        private int lastZoomLevel = -1;
        private Vector2Int zoomBounds = Vector2Int.zero;

        protected LayerURLPropertyData urlPropertyData = new();
        
        protected override void Awake()
        {
            base.Awake();  

            if (timeController == null)
            {
                timeController = gameObject.AddComponent<ATMDataController>();
                ATMDataHandler = (a) => UpdateCurrentZoomLayer(true);
                timeController.ChangeYear += ATMDataHandler;
            }
        }

        void Update()
        {
            if (zoomLevel < 0)
                return;

            zoomLevel = CalculateZoomLevel();
            zoomLevel = Mathf.Clamp(zoomLevel, zoomBounds.x, zoomBounds.y);
            UpdateCurrentZoomLayer();
        }

        private void UpdateCurrentZoomLayer(bool force = false)
        {
            if(ATMTileDataLayers == null || zoomLevel < 0)
                return;

            if (zoomLevel != lastZoomLevel || force)
            {
                CartesianTiles.TileHandler handler = GetComponentInParent<CartesianTiles.TileHandler>();
                //get the current enabled zoom layer and update it with setvisibletilesdirty
                int index = GetZoomLayerIndex(zoomLevel);
                for (int i = 0; i < ATMTileDataLayers.Length; i++)
                    if (i != index)
                        ATMTileDataLayers[i].isEnabled = false;
                        //if (handler.layers.Contains(ATMTileDataLayers[i]))
                        //    handler.RemoveLayer(ATMTileDataLayers[i]);
               

                ATMTileDataLayers[index].isEnabled = true;
                //if (!handler.layers.Contains(ATMTileDataLayers[index]))
                //    handler.AddLayer(ATMTileDataLayers[index]);
                ATMTileDataLayers[index].SetVisibleTilesDirty();
                lastZoomLevel = zoomLevel;
            }
        }

        protected override void Start()
        {
            base.Start();
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);

            ATMLayerGameObject parent = transform.parent.GetComponent<ATMLayerGameObject>();
            if (parent != null)
                return;

            
            ATMTileDataLayer current = GetComponent<ATMTileDataLayer>();
            TextureProjectorBase projectorPrefab = current.ProjectorPrefab;
            current.SetZoomLevel(-1);
            //current.enabled = false;
            current.isEnabled = false;

            CartesianTiles.TileHandler handler = GetComponentInParent<CartesianTiles.TileHandler>();
            //handler.RemoveLayer(current);

            zoomLevel = 16;
            zoomBounds = timeController.GetZoomBoundsAllYears();
            int length = zoomBounds.y - zoomBounds.x + 1;
            ATMTileDataLayers = new ATMTileDataLayer[length];
            for (int i = 0; i < ATMTileDataLayers.Length; i++)
            {
                GameObject zoomLayerObject = new GameObject((zoomBounds.x + i).ToString());
                zoomLayerObject.AddComponent<XyzTiles>();
                zoomLayerObject.AddComponent<WorldTransform>();
                zoomLayerObject.AddComponent<ChildWorldTransformShifter>();
                
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
                if (!handler.layers.Contains(ATMTileDataLayers[i]))
                    handler.AddLayer(ATMTileDataLayers[i]);

            //    if(i!=index)
            //    ATMTileDataLayers[i].isEnabled = false;

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
            float viewDistance = camPosition.y; //lets keep it orthographic?
            var unityCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                camPosition.x,
                camPosition.z,
                0
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
            float latitude = (float)coord.Points[0];
            float cosLatitude = Mathf.Cos(latitude * Mathf.Deg2Rad); //to rad

            //https://wiki.openstreetmap.org/wiki/Zoom_levels
            float numerator = equatorialCircumference * cosLatitude;
            float zoomLevel = Mathf.Log(numerator / viewDistance) / log2x;

            return Mathf.RoundToInt(zoomLevel);
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