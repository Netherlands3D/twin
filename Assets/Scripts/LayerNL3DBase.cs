using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.Inpector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI.Inpector
{
    public abstract class LayerNL3DBase : MonoBehaviour
    {
        public LayerUI2 UI { get; set; }

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
        public Image Icon { get; set; }

        public string Name
        {
            get => gameObject.name;
            set => gameObject.name = value;
        }

        public LayerNL3DBase Parent { get; private set; }
        public List<LayerNL3DBase> Children { get; private set; } = new();
        public int Depth { get; private set; } = 0;


        protected virtual void OnEnable()
        {
            LayersUI.Layers.Add(this);
        }

        protected virtual void OnDisable()
        {
            LayersUI.Layers.Remove(this);
        }

        public void LinkListeners()
        {
            UI.EnabledToggle.RegisterValueChangedCallback(OnLayerToggleChanged);
        }

        public void UnLinkListeners()
        {
            UI.EnabledToggle.UnregisterValueChangedCallback(OnLayerToggleChanged);
        }

        private void OnLayerToggleChanged(ChangeEvent<bool> evt)
        {
            LayerEnabled = evt.newValue;
        }

        public abstract void OnLayerEnableChanged(bool value);

        public void SetParent(LayerNL3DBase newParent, int childIndex = -1)
        {
            if (Parent)
                Parent.Children.Remove(this);

            Parent = newParent;
            if (childIndex >= 0 && childIndex < newParent.Children.Count)
                newParent.Children.Insert(childIndex, this);
            else
                newParent.Children.Add(this);

            Depth = Parent.Depth + 1;
            
            UpdateUI();
        }

        private void UpdateUI()
        {
            UI?.UpdateLayerUI();
        }
    }
}