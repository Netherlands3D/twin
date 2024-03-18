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
            get
            {
                return (layer && layer.isEnabled);
            }
            set
            {
                if (layer && layer.isEnabled != value)
                    layer.isEnabled = value;

                if(ReferencedProxy && ReferencedProxy.UI)
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

            if(Application.isPlaying && tileHandler && layer)
                tileHandler.RemoveLayer(layer);
        }
    }
}