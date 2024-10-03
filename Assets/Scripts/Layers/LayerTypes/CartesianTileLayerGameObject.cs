using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(CartesianTiles.Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject
    {
        private CartesianTiles.Layer layer;
        private CartesianTiles.TileHandler tileHandler;

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (layer && layer.isEnabled != isActive)
                layer.isEnabled = isActive;
        }
        
        protected virtual void Awake()
        {
            tileHandler = FindAnyObjectByType<CartesianTiles.TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<CartesianTiles.Layer>();

            tileHandler.AddLayer(layer);
        }

        protected void OnDestroy()
        {
            if(Application.isPlaying && tileHandler && layer)
                tileHandler.RemoveLayer(layer);
        }
    }
}