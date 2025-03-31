using Netherlands3D.LayerStyles;

namespace Netherlands3D.Twin.Layers
{
    public struct LayerFeature
    {
        // Geometry is used loosely here - this means _anything_ that represents the physical aspect of a feature
        // individual layer types are expected to know what type they are using, and thus how to retrieve it.
        public readonly object Geometry;
        public readonly ExpressionContext Attributes;

        private LayerFeature(object geometry, ExpressionContext attributes = null)
        {
            this.Geometry = geometry;
            Attributes = attributes ?? new ExpressionContext();
        }

        /// <param name="geometry">
        /// Geometry is used loosely here - this means _anything_ that represents the physical aspect of a feature
        /// individual layer types are expected to know what type they are using, and thus how to retrieve it.
        /// </param>
        public static LayerFeature Create(LayerGameObject layer, object geometry)
        {
            var expressionContext = new ExpressionContext
            {
                { "nl3d_layer_id", layer.LayerData.Id.ToString() },
                { "nl3d_layer_name", layer.LayerData.Name }
            };
            
            return new LayerFeature(geometry, expressionContext);
        }
    }
}