using Netherlands3D.Twin._Functionalities.Wms.Scripts;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin._Functionalities.Wms
{
    public class AtmMapController : MonoBehaviour
    {
        public ATMMapLayerGameObject layerPrefab;
        private ATMLayerManager layerManager;

        private void Start()
        {
            layerManager = new ATMLayerManager(gameObject.AddComponent<ATMDataController>());
            layerManager.CreateTileHandlerForEachZoomLevel(transform, layerPrefab);
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