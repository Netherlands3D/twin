using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Samplers;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties
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