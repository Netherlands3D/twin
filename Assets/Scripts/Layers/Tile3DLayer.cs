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
        public override bool IsActiveInScene => layer.isEnabled;

        private void Awake()
        {
            layer = GetComponent<CartesianTiles.Layer>();
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

        public override void SetEnabled(bool isActiveInHierarchy)
        {
            if (layer.isEnabled != isActiveInHierarchy)
                layer.isEnabled = isActiveInHierarchy;
            UI.UpdateLayerUI();
        }

        private void OnLayerEnabled()
        {
            // IsActiveSelf = true;
        }

        private void OnLayerDisabled()
        {
            // IsActiveSelf = false;
        }
    }
}