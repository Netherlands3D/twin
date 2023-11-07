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

        // public LayerNL3DBase Parent { get; private set; }
        // public List<LayerNL3DBase> Children { get; private set; } = new();


        protected virtual void OnEnable()
        {
            LayerManager.AllLayers.Add(this);
        }

        protected virtual void OnDisable()
        {
            LayerManager.AllLayers.Remove(this);
        }

        public abstract void OnLayerEnableChanged(bool value);

        // public void SetParent(LayerUI newParent, int childIndex = -1)
        // {
        //     if (newParent == null)
        //         UI.transform.SetParent(UI.LayerBaseTransform);
        //     else
        //         UI.transform.SetParent(newParent.transform);
        //
        //     UI.transform.SetSiblingIndex(childIndex);
        //
        //     RecalculateDepthValuesRecursively();
        // }
    }
}