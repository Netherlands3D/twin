using Netherlands3D.Coordinates;
using Netherlands3D.Tilekit;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.TileStreaming
{
    [RequireComponent(typeof(TileStreamer))]
    public sealed class TileStreamerLayerGameObject : LayerGameObject
    {
        public override BoundingBox Bounds => StandardBoundingBoxes.RDBounds;

        private TileStreamer tileStreamer;
        private PointerToWorldPosition pointerToWorldPosition;
        private bool isReady;

        protected override void OnVisualizationInitialize()
        {
            tileStreamer = GetComponent<TileStreamer>();
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
            isReady = true;
        }

        protected override void OnVisualizationReady()
        {
            isReady = true;
            
            tileStreamer.SetActiveCamera(App.Cameras.ActiveCamera);
            App.Cameras.OnSwitchCamera.AddListener(tileStreamer.SetActiveCamera);

            if (!LayerData.ActiveInHierarchy) return;
            
            tileStreamer.StartStreaming();
        }

        protected override void UnregisterEventListeners()
        {
            App.Cameras.OnSwitchCamera.RemoveListener(tileStreamer.SetActiveCamera);
            base.UnregisterEventListeners();
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (isActive)
            {
                gameObject.SetActive(true);
                if (isReady)
                {
                    tileStreamer.StartStreaming();
                }
            }
            else
            {
                if (isReady)
                {
                    tileStreamer.StopStreaming();
                }
                gameObject.SetActive(false);
            }
        }
    }
}
