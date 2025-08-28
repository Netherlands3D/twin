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
        ILayerBuilder FromUrl(Uri url);
        ILayerBuilder FromUrl(string url);
        ILayerBuilder FromFile(Uri url);
        ILayerBuilder NamedAs(string name);
        ILayerBuilder WithColor(Color color);
        ILayerBuilder At(Uri url);
        ILayerBuilder ChildOf(LayerData parent);
        ILayerBuilder WithCredentials(StoredAuthorization creds);
        ILayerBuilder AddProperty(LayerPropertyData property);
        ILayerBuilder AddProperties(params LayerPropertyData[] properties);
        ILayerBuilder PositionedAt(Vector3 position);
        ILayerBuilder Rotated(Quaternion rotation);
        ILayerBuilder SetDefaultStyling(Symbolizer symbolizer);
        ILayerBuilder AddStyle(LayerStyle style);
        LayerData Build(LayerGameObject ontoReference);
        ILayerBuilder PostBuild(Action<LayerData> callback);
    }
}