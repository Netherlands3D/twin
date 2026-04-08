using System;

namespace Netherlands3D.Twin.Layers.Properties
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PropertySectionAttribute : Attribute
    {
        public Type RequiredPropertyType { get; }
        public string SubType { get; }

        public PropertySectionAttribute(Type requiredPropertyType, string subType = null)
        {
            RequiredPropertyType = requiredPropertyType;
            SubType = subType;
        }
    }
}