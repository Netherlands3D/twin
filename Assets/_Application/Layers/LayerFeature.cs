using Netherlands3D.LayerStyles;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public class LayerFeature
    {
        public Component Component;
        public readonly ExpressionContext Attributes = new();

        public LayerFeature(Component component, ExpressionContext attributes = null)
        {
            Component = component;
            Attributes = attributes ?? Attributes;
        }

        public static LayerFeature Create(LayerGameObject layer, Component component)
        {
            var expressionContext = new ExpressionContext
            {
                { "nl3d_layer_id", layer.LayerData.Id.ToString() },
                { "nl3d_layer_name", layer.LayerData.Name }
            };
            
            return new LayerFeature(component, expressionContext);
        }
    }
}