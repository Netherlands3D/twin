using System;
using Netherlands3D.TileSystem;
using UnityEngine;
using Netherlands3D.CartesianTiles;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    [RequireComponent(typeof(CartesianTiles.Layer))]
    public class Tile3DLayer : ReferencedLayer
    {
        private CartesianTiles.Layer layer;
        
        public override bool IsActiveInScene
        {
            get => layer.isEnabled;
            set
            {
                if (layer.isEnabled != value)
                    layer.isEnabled = value;
                ReferencedProxy.UI.UpdateLayerUI();
            }
        }

        protected override void Start()
        {
            base.Start();
            layer = GetComponent<CartesianTiles.Layer>();
        }
    }
}