using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.TileSystem;
using Netherlands3D.Twin.UI.Inpector;
using Unity.VisualScripting;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Layer))]
    public class Tile3DLayer : LayerNL3DBase
    {
        private Layer layer;

        private void Awake()
        {
            layer = GetComponent<Layer>();
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