using System;
using System.Reflection;
using Netherlands3D.Rendering;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin._Functionalities.Wms.Scripts
{
    public class ATMLayerManager : IDisposable
    {
        private ATMTileDataLayer[] ATMTileDataLayers => atmTileDataLayers;

        private int zoomLevel = -1;
        private int lastZoomLevel = -1;
        private ATMDataController timeController;
        private ATMTileDataLayer[] atmTileDataLayers;

        public ATMLayerManager(ATMDataController timeController)
        {
            this.timeController = timeController;
            timeController.ChangeYear += OnYearChanged;
        }

        private void OnYearChanged(int a)
        {
            UpdateCurrentZoomLayer(true);
        }

        public void SwitchLayerToCurrentZoomLevel(bool force = false)
        {
            zoomLevel = timeController.CalculateZoomLevel();

            UpdateCurrentZoomLayer(force);
        }

        public void UpdateCurrentZoomLayer(bool force = false)
        {
            if (ATMTileDataLayers == null) return;
            if (zoomLevel == lastZoomLevel && !force) return;

            //get the current enabled zoom layer and update it with setvisibletilesdirty
            int index = timeController.GetZoomLayerIndex(zoomLevel);

            ActivateLayer(zoomLevel);
            ATMTileDataLayers[index].SetVisibleTilesDirty();

            lastZoomLevel = zoomLevel;
        }

        public void CreateTileHandlerForEachZoomLevel(Transform parent, ATMMapLayerGameObject layerGameObjectPrefab)
        {
            var zoomBounds = timeController.GetZoomBoundsAllYears();
            int numberOfZoomLevels = zoomBounds.y - zoomBounds.x + 1;
            atmTileDataLayers = new ATMTileDataLayer[numberOfZoomLevels];
            for (int i = 0; i < ATMTileDataLayers.Length; i++)
            {
                CreateTileLayerForDataLayer(parent, i, zoomBounds.x + i, layerGameObjectPrefab);
            }
        }

        public void CreateTileHandlerForEachZoomLevel(Transform parent, TextureProjectorBase projectorPrefab)
        {
            var zoomBounds = timeController.GetZoomBoundsAllYears();
            int numberOfZoomLevels = zoomBounds.y - zoomBounds.x + 1;
            atmTileDataLayers = new ATMTileDataLayer[numberOfZoomLevels];
            for (int i = 0; i < ATMTileDataLayers.Length; i++)
            {
                CreateTileLayerForDataLayer(parent, i, zoomBounds.x + i, projectorPrefab);
            }
        }

        private void ActivateLayer(int zoomLevel)
        {
            int index = timeController.GetZoomLayerIndex(zoomLevel);
            for (int i = 0; i < ATMTileDataLayers.Length; i++)
            {
                ATMTileDataLayers[i].isEnabled = i == index;
                
                if (i != index)
                {
                    ATMTileDataLayers[i].CancelTiles();
                }
            }

            // I know .. reflection. But this will ensure that if another zoom level enables, that the tilesizes doesn't
            // go mad. And trying to disable and enable all gameobjects that have a cartesian tile layer seems like
            // overkill and might have issues due to onEnable and onDisable's triggering 
            MethodInfo privateMethod = typeof(CartesianTiles.TileHandler).GetMethod("GetTilesizes", BindingFlags.NonPublic | BindingFlags.Instance);
            if (privateMethod != null)
            {
                privateMethod.Invoke(Object.FindObjectOfType<CartesianTiles.TileHandler>(), null);
            }
        }

        private void CreateTileLayerForDataLayer(Transform parent, int i, int forZoomLevel, TextureProjectorBase projectorPrefab)
        {
            GameObject zoomLayerObject = new GameObject(forZoomLevel.ToString());
            zoomLayerObject.AddComponent<XyzTiles>();
            WorldTransform wt = zoomLayerObject.AddComponent<WorldTransform>();
            ChildWorldTransformShifter cts = zoomLayerObject.AddComponent<ChildWorldTransformShifter>();
            wt.SetShifter(cts);
            Object.Destroy(zoomLayerObject.GetComponent<GameObjectWorldTransformShifter>());
                
            ATMTileDataLayers[i] = zoomLayerObject.AddComponent<ATMTileDataLayer>();
                
            ATMTileDataLayer zoomLayer = ATMTileDataLayers[i].GetComponent<ATMTileDataLayer>();
            zoomLayer.ProjectorPrefab = projectorPrefab;
            zoomLayer.SetDataController(timeController);                
            zoomLayer.SetZoomLevel(forZoomLevel);
            zoomLayer.tileSize = timeController.GetTileSizeForZoomLevel(zoomLayer.ZoomLevel);
            zoomLayerObject.AddComponent<ATMLayerGameObject>();
            ATMTileDataLayers[i].transform.SetParent(parent, false);
        }

        private void CreateTileLayerForDataLayer(Transform parent, int i, int forZoomLevel, ATMMapLayerGameObject layerGameObjectPrefab)
        {
            var tileSize = timeController.GetTileSizeForZoomLevel(forZoomLevel);

            // hack the prefab because we need the tileSize to be set before instantiation
            layerGameObjectPrefab.GetComponent<ATMTileDataLayer>().tileSize = tileSize;
            
            var zoomLayerObject = Object.Instantiate(layerGameObjectPrefab, parent, false);
            zoomLayerObject.gameObject.name = forZoomLevel.ToString();

            ATMTileDataLayer zoomLayer = zoomLayerObject.GetComponent<ATMTileDataLayer>();
            ATMTileDataLayers[i] = zoomLayer;
            
            zoomLayer.SetDataController(timeController);                
            zoomLayer.SetZoomLevel(forZoomLevel);
            zoomLayer.tileSize = tileSize;
        }

        public void Dispose()
        {
            timeController.ChangeYear -= OnYearChanged;
        }
    }
}