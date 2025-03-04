using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject
    {
        public override BoundingBox Bounds =>
            StandardBoundingBoxes.RDBounds; //assume we cover the entire RD bounds area

        private Layer layer;
        private TileHandler tileHandler;

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (!layer) return;
            if (layer.isEnabled == isActive) return;
            
            layer.isEnabled = isActive;
        }

        protected virtual void Awake()
        {
            tileHandler = FindAnyObjectByType<TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<Layer>();

            tileHandler.AddLayer(layer);
        }

        protected virtual void OnDestroy()
        {
            if (!Application.isPlaying) return;
            if (!tileHandler) return;
            if (!layer) return;

            tileHandler.RemoveLayer(layer);
        }
    }
}