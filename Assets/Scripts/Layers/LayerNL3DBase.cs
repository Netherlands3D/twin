using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.Events;

// using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public abstract class LayerNL3DBase : MonoBehaviour
    {
        public LayerUI UI { get; set; }

        private bool layerEnabled = true;

        public bool LayerEnabled
        {
            get => layerEnabled;
            set
            {
                layerEnabled = value;
                OnLayerEnableChanged(value);
                UI?.UpdateLayerUI();
            }
        }
        
        protected virtual void OnEnable()
        {
            LayerManager.AllLayers.Add(this);
        }

        protected virtual void OnDisable()
        {
            LayerManager.AllLayers.Remove(this);
        }

        public abstract void OnLayerEnableChanged(bool value);
    }
}