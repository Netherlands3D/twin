using System.Collections.Generic;

namespace Netherlands3D.LayerStyles.Expressions
{
    public interface IFeatureForExpression
    {
        object Geometry { get; }
        Dictionary<string, string> Attributes { get; }
        string GetAttribute(string attributeKey);
    }
}