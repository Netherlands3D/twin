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
            LayerEnabled = layer.isEnabled;
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
            LayerEnabled = true;
        }

        private void OnLayerDisabled()
        {
            LayerEnabled = false;
        }

        public override void OnLayerEnableChanged(bool value)
        {
            if (layer.isEnabled != value)
                layer.isEnabled = value;
        }
    }
}