using System;

namespace Netherlands3D.UI.Behaviours
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InspectorPanelAttribute : Attribute
    {
        public Type RequiredInspectorType { get; }
        public InspectorPanelAttribute(Type requiredPropertyType)
        {
            RequiredInspectorType = requiredPropertyType;
        }
    }
}