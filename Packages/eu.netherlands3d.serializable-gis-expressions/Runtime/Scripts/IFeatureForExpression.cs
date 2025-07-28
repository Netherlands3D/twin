using System.Collections.Generic;

namespace Netherlands3D.SerializableGisExpressions
{
    public interface IFeatureForExpression
    {
        object Geometry { get; }
        Dictionary<string, string> Attributes { get; }
        string GetAttribute(string attributeKey);
    }
}