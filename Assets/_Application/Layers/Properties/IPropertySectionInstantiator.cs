using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public interface IPropertySectionInstantiator
    {
        //defines the property section index starting from the right
        uint SectionIndex { get; }
        void AddToProperties(RectTransform properties);
    }
}
