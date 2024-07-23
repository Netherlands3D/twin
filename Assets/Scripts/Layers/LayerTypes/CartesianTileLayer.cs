using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(CartesianTiles.Layer))]
    public class CartesianTileLayer : ReferencedLayer
    {
        private CartesianTiles.Layer layer;
        private CartesianTiles.TileHandler tileHandler;

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (layer && layer.isEnabled != isActive)
                layer.isEnabled = isActive;
        }
        
        protected void Awake()
        {
            tileHandler = FindAnyObjectByType<CartesianTiles.TileHandler>();
            transform.SetParent(tileHandler.transform);
            layer = GetComponent<CartesianTiles.Layer>();

            tileHandler.AddLayer(layer);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if(Application.isPlaying && tileHandler && layer)
                tileHandler.RemoveLayer(layer);
        }
    }
}