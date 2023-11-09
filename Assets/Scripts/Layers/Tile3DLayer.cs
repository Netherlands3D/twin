using System;
using Netherlands3D.TileSystem;
using UnityEngine;
using Netherlands3D.CartesianTiles;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    [RequireComponent(typeof(CartesianTiles.Layer))]
    public class Tile3DLayer : LayerNL3DBase
    {
        private CartesianTiles.Layer layer;
        public override bool IsActiveInScene
        {
            get => layer.isEnabled;
            set
            {
                if (layer.isEnabled != value)
                    layer.isEnabled = value;
                UI.UpdateLayerUI();
            }
        }

        private void Awake()
        {
            layer = GetComponent<CartesianTiles.Layer>();
        }
    }
}