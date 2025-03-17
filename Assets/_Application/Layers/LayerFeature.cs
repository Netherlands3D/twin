using Netherlands3D.LayerStyles;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public class LayerFeature<T> where T : Component
    {
        public T Component;
        public readonly ExpressionContext Attributes = new();

        public LayerFeature(T component, ExpressionContext attributes = null)
        {
            Component = component;
            Attributes = attributes ?? Attributes;
        }
    }
}