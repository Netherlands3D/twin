using System;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class ToggleScatterPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private ToggleScatterPropertySection propertySectionPrefab;
        private static ToggleScatterPropertySection staticPropertySectionPrefab;
        public ToggleScatterPropertySection PropertySection { get; private set; }

        private void Awake()
        {
            if(propertySectionPrefab != null) //todo: this is a big hack
                staticPropertySectionPrefab = propertySectionPrefab;
        }

        public void AddToProperties(RectTransform properties)
        {
            if (!staticPropertySectionPrefab) return;

            PropertySection = Instantiate(staticPropertySectionPrefab, properties);
            print("assigning layer " + GetComponent<ReferencedLayer>());
            PropertySection.Layer = GetComponent<ReferencedLayer>();
        }
    }
}