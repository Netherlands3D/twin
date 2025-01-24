using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Samplers;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class JumpToCameraPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private JumpToCameraPropertySection prefab;
        public JumpToCameraPropertySection PropertySection { get; private set; }

        public void AddToProperties(RectTransform properties)
        {
            if (!ScatterMap.Instance.ToggleScatterPropertySection) return;

            PropertySection = Instantiate(prefab, properties);
            PropertySection.LayerGameObject = GetComponent<HierarchicalObjectLayerGameObject>();
        }
    }
}