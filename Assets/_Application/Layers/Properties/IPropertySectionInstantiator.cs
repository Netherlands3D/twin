using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public interface IPropertySectionInstantiator
    {
        uint SectionIndex { get; }
        void AddToProperties(RectTransform properties);
    }
}
