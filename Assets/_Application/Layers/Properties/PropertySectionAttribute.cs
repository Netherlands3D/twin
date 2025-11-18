using System;

namespace Netherlands3D.Twin.Layers.Properties
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PropertySectionAttribute : Attribute
    {
        public Type PropertyType { get; }

        public PropertySectionAttribute(Type propertyType)
        {
            PropertyType = propertyType;
        }
    }
}