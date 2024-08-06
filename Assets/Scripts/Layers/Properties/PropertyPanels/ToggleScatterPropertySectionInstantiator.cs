using System;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ToggleScatterPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        public ToggleScatterPropertySection PropertySection { get; private set; }

        public void AddToProperties(RectTransform properties)
        {
            if (!ScatterMap.Instance.ToggleScatterPropertySection) return;

            PropertySection = Instantiate(ScatterMap.Instance.ToggleScatterPropertySection, properties);
            PropertySection.LayerGameObject = GetComponent<LayerGameObject>();
        }
    }
}