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
                UpdateUI();
            }
        }

        public Color Color { get; set; } = Color.blue;
        public Sprite Icon { get; set; }

        public LayerNL3DBase Parent { get; private set; }
        public List<LayerNL3DBase> Children { get; private set; } = new();
        public int Depth { get; private set; } = 0;


        protected virtual void OnEnable()
        {
            LayerManager.AllLayers.Add(this);
        }

        protected virtual void OnDisable()
        {
            LayerManager.AllLayers.Remove(this);
        }

        public abstract void OnLayerEnableChanged(bool value);

        public void SetParent(LayerNL3DBase newParent, int childIndex = -1)
        {
            if (Parent)
                Parent.Children.Remove(this);

            Parent = newParent;
            if (newParent != null)
            {
                if (childIndex >= 0 && childIndex < newParent.Children.Count)
                    newParent.Children.Insert(childIndex, this);
                else
                    newParent.Children.Add(this);
            }

            RecalculateDepthValuesRecursively();
        }

        private void RecalculateDepthValuesRecursively()
        {
            if (Parent)
                Depth = Parent.Depth + 1;
            else
                Depth = 0;

            foreach (var child in Children)
                child.RecalculateDepthValuesRecursively();

            UpdateUI();
        }

        private void UpdateUI()
        {
            UI?.UpdateLayerUI();
        }
    }
}