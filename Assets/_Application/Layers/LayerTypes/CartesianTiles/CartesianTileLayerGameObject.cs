using UnityEngine;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin.Utility;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    [RequireComponent(typeof(Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject
    {
        public override BoundingBox Bounds => BoundingBox.RDBounds; //assume we cover the entire RD bounds area
        
        private Layer layer;
        private Netherlands3D.CartesianTiles.TileHandler tileHandler;

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (layer && layer.isEnabled != isActive)
                layer.isEnabled = isActive;
        }
        
        protected virtual void Awake()
        {
            tileHandler = FindAnyObjectByType<Netherlands3D.CartesianTiles.TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<Layer>();

            tileHandler.AddLayer(layer);
        }

        protected virtual void OnDestroy()
        {
            if(Application.isPlaying && tileHandler && layer)
                tileHandler.RemoveLayer(layer);
        }
    }
}