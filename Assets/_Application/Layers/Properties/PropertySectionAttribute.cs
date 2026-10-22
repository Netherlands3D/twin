using System;

namespace Netherlands3D.Twin.Layers.Properties
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PropertySectionAttribute : Attribute
    {
        public Type RequiredPropertyType { get; }
        public PropertySectionCategory Category { get; }
        public int Order { get; }

        public PropertySectionAttribute(Type requiredPropertyType,  PropertySectionCategory category, int order = 0)
        {
            RequiredPropertyType = requiredPropertyType;
            Category = category;
            Order = order;
        }
    }
}