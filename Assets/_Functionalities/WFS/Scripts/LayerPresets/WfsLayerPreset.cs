using System;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Functionalities.Wfs.LayerPresets
{
    [LayerPreset("wfs-layer")]
    public sealed class WfsLayerPreset : ILayerPreset<WfsLayerPreset.Args>
    {
        private const string PrefabIdentifier = "b1bd3a7a50cb3bd4bb3236aadf5c32b6";

        public sealed class Args : LayerPresetArgs<WfsLayerPreset>
        {
            public Uri FeatureUrl { get; }
            public string Title { get; }
            public LayerData Parent { get; }

            public Args(
                Uri featureUrl, 
                string title
            ) {
                FeatureUrl = featureUrl ?? throw new ArgumentNullException(nameof(featureUrl));
                Title = !string.IsNullOrWhiteSpace(title) ? title
                    : throw new ArgumentException("Title is required.", nameof(title));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            var color = LayerColor.Random();

            var styling = new Symbolizer();
            styling.SetFillColor(color);
            styling.SetStrokeColor(color);

            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(args.Title)
                .ChildOf(args.Parent)
                .WithColor(color)
                .SetDefaultStyling(styling)
                .AddProperty(new LayerURLPropertyData(args.FeatureUrl));
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}