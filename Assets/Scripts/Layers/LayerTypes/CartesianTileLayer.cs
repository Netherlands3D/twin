using System;
using UnityEngine;
using Netherlands3D.CartesianTiles;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    [RequireComponent(typeof(CartesianTiles.Layer))]
    public class CartesianTileLayer : ReferencedLayer
    {
        private CartesianTiles.Layer layer;
        private CartesianTiles.TileHandler tileHandler;
        
        public override bool IsActiveInScene
        {
            get => layer.isEnabled;
            set
            {
                if (layer.isEnabled != value)
                    layer.isEnabled = value;
                ReferencedProxy.UI.MarkLayerUIAsDirty();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            tileHandler = GetComponentInParent<CartesianTiles.TileHandler>();
            layer = GetComponent<CartesianTiles.Layer>();

            tileHandler.AddLayer(layer);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GetComponentInParent<CartesianTiles.TileHandler>().RemoveLayer(layer);
        }
    }
}