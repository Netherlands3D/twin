using System;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public interface ILayerBuilder
    {
        ILayerBuilder OfType(string type);
        ILayerBuilder NamedAs(string name);
        ILayerBuilder WithColor(Color color);
        ILayerBuilder WithCredentials(StoredAuthorization creds);
        ILayerBuilder AddProperty(LayerPropertyData property);
        ILayerBuilder AddProperties(params LayerPropertyData[] properties);
        ILayerBuilder PositionedAt(Vector3 position);
        ILayerBuilder Rotated(Quaternion rotation);
        ILayerBuilder SetDefaultStyling(Symbolizer symbolizer);
        LayerData Build();
        ILayerBuilder WhenBuilt(Action<LayerData> callback);
    }
}