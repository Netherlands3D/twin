using System;

namespace Netherlands3D.Twin.Layers.Properties
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PropertySectionAttribute : Attribute
    {
        public Type RequiredPropertyType { get; }

        public PropertySectionAttribute(Type requiredPropertyType)
        {
            RequiredPropertyType = requiredPropertyType;
        }
    }
}