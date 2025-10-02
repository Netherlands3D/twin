using System;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Functionalities.GeoJSON.LayerPresets
{
    [LayerPreset("geojson")]
    public sealed class GeoJSONPreset : ILayerPreset
    {
        private const string PrefabIdentifier = "e46381d2665c69245b2475c986f6d0c4";

        public sealed class Args : LayerPresetArgs
        {
            public string Name { get; }
            public Uri Url { get; }

            public Args(string name, Uri url) 
            {
                Name = name;
                Url = url ?? throw new ArgumentNullException(nameof(url));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args)
        {
            if (args is not Args geoJsonArgs)
            {
                throw new ArgumentException($"Expected {nameof(Args)} for preset geojson.");
            }

            var color = LayerColor.Random();

            var styling = new Symbolizer();
            styling.SetFillColor(color);
            styling.SetStrokeColor(color);

            return builder
                .OfType(PrefabIdentifier)
                .WithColor(color)
                .NamedAs(geoJsonArgs.Name)
                .SetDefaultStyling(styling)
                .AddProperty(new LayerURLPropertyData { Data = geoJsonArgs.Url });
        }
    }
}