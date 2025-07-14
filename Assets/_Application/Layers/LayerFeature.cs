using System.Collections.Generic;
using Netherlands3D.LayerStyles.Expressions;

namespace Netherlands3D.Twin.Layers
{
    public class LayerFeature : IFeatureForExpression
    {
        // Geometry is used loosely here - this means _anything_ that represents the physical aspect of a feature
        // individual layer types are expected to know what type they are using, and thus how to retrieve it.
        public object Geometry { get; }
        public Dictionary<string, string> Attributes { get; }

        private LayerFeature(object geometry, Dictionary<string, string> attributes = null)
        {
            this.Geometry = geometry;
            Attributes = attributes ?? new Dictionary<string, string>();
        }

        /// <param name="geometry">
        /// Geometry is used loosely here - this means _anything_ that represents the physical aspect of a feature
        /// individual layer types are expected to know what type they are using, and thus how to retrieve it.
        /// </param>
        public static LayerFeature Create(LayerGameObject layer, object geometry)
        {
            var expressionContext = new Dictionary<string, string>
            {
                { "nl3d_layer_id", layer.LayerData.Id.ToString() },
                { "nl3d_layer_name", layer.LayerData.Name }
            };
            
            return new LayerFeature(geometry, expressionContext);
        }

        /// <param name="geometry">
        /// Geometry is used loosely here - this means _anything_ that represents the physical aspect of a feature
        /// individual layer types are expected to know what type they are using, and thus how to retrieve it.
        /// </param>
        public static LayerFeature Create(object geometry)
        {
            return new LayerFeature(geometry, new Dictionary<string, string>());
        }

        public string GetAttribute(string id)
        {
            return Attributes.GetValueOrDefault(id);
        }
    }
}