using System;
using Netherlands3D.Twin._Functionalities.Wms.Scripts;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin._Functionalities.Wms
{
    public class AtmMapController : MonoBehaviour
    {
        public ATMMapLayerGameObject layerPrefab;
        private ATMLayerManager layerManager;

        private void Awake()
        {
            layerManager = new ATMLayerManager(gameObject.AddComponent<ATMDataController>());
            layerManager.CreateTileHandlerForEachZoomLevel(transform, layerPrefab);
        }

        private void Start()
        {
            layerManager.SwitchLayerToCurrentZoomLevel(true);
        }

        void Update()
        {
            layerManager?.SwitchLayerToCurrentZoomLevel();
        }

        private void OnDestroy()
        {
            layerManager?.Dispose();
        }
    }
}