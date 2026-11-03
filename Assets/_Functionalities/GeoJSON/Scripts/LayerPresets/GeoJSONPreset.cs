using System;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Functionalities.GeoJSON.LayerPresets
{
    [LayerPreset("geojson")]
    public sealed class GeoJSONPreset : ILayerPreset<GeoJSONPreset.Args>
    {
        private const string PrefabIdentifier = "e46381d2665c69245b2475c986f6d0c4";

        public sealed class Args : LayerPresetArgs<GeoJSONPreset>
        {
            public string Name { get; }
            public Uri Url { get; }
            
            public bool RenderingGeometryEnabled { get; set; }

            public Args(string name, Uri url, bool renderingGeometryEnabled = true) 
            {
                Name = name;
                Url = url ?? throw new ArgumentNullException(nameof(url));
                RenderingGeometryEnabled = renderingGeometryEnabled;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args geoJsonArgs)
        {
            var color = LayerColor.Random();

            var styling = new Symbolizer();

            return builder
                .OfType(PrefabIdentifier)
                .WithColor(color)
                .NamedAs(geoJsonArgs.Name)
                .SetDefaultStyling(styling)
                .AddProperty(new LayerURLPropertyData { Url = geoJsonArgs.Url })
                .AddProperty(new GeoJSONPropertyData(default, geoJsonArgs.RenderingGeometryEnabled));
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args)
            => Apply(builder, (Args)args);
    }
}