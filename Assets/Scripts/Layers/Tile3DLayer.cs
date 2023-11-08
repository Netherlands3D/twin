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

        private void Awake()
        {
            layer = GetComponent<CartesianTiles.Layer>();
        }

        public override bool IsEnabled
        {
            get { return layer.isEnabled; }
            set
            {
                if (layer.isEnabled != value)
                    layer.isEnabled = value;
                UI.UpdateLayerUI();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            layer.onLayerEnabled.AddListener(OnLayerEnabled);
            layer.onLayerDisabled.AddListener(OnLayerDisabled);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            layer.onLayerEnabled.RemoveListener(OnLayerEnabled);
            layer.onLayerDisabled.RemoveListener(OnLayerDisabled);
        }

        private void OnLayerEnabled()
        {
            if (UI)
                UI.LayerEnabled = true;
        }

        private void OnLayerDisabled()
        {
            if (UI)
                UI.LayerEnabled = false;
        }
    }
}