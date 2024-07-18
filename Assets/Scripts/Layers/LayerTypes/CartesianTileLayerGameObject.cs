using System;
using UnityEngine;
using Netherlands3D.CartesianTiles;

namespace Netherlands3D.Twin.Layers
{
    [RequireComponent(typeof(CartesianTiles.Layer))]
    public class CartesianTileLayerGameObject : LayerGameObject
    {
        private CartesianTiles.Layer layer;
        private CartesianTiles.TileHandler tileHandler;

        protected override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (layer && layer.isEnabled != isActive)
                layer.isEnabled = isActive;
        }
        
        protected override void Awake()
        {
            base.Awake();
            tileHandler = GameObject.FindAnyObjectByType<CartesianTiles.TileHandler>();
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