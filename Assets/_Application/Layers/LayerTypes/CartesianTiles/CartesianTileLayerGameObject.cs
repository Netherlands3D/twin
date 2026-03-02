using Netherlands3D.CartesianTiles;
using Netherlands3D.Services;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject
    {
        public override BoundingBox Bounds => StandardBoundingBoxes.RDBounds; //assume we cover the entire RD bounds area

        public Layer Layer => layer;

        private Layer layer;
        private TileHandler tileHandler;

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (!layer || layer.isEnabled == isActive) return;

            layer.isEnabled = isActive;
        }

        protected override void OnVisualizationInitialize()
        {
            tileHandler = FindAnyObjectByType<TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<Layer>();

            tileHandler.AddLayer(layer);  
        }

        public override void OnSelect(LayerData layer)
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.ShowVisibilityPanel(true);
        }

        public override void OnDeselect(LayerData layer)
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.ShowVisibilityPanel(false);
        }

        protected void OnDestroy()
        {
            if (Application.isPlaying && tileHandler && layer)
            {
                tileHandler.RemoveLayer(layer);
            }
        }
    }
}