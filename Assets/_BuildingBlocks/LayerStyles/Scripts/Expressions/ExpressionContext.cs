using Netherlands3D.Twin.Layers;

namespace Netherlands3D.LayerStyles.Expressions
{
    public class ExpressionContext
    {
        public LayerFeature Feature { get; }

        public ExpressionContext(LayerFeature feature)
        {
            this.Feature = feature;
        }
    }
}