using System;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Functionalities.Wfs.LayerPresets
{
    [LayerPreset("wfs-layer")]
    public sealed class WfsLayer : ILayerPreset
    {
        private const string PrefabIdentifier = "b1bd3a7a50cb3bd4bb3236aadf5c32b6";

        public sealed class Args : LayerPresetArgs
        {
            public Uri FeatureUrl { get; }
            public string Title { get; }
            public LayerData Parent { get; }

            public Args(
                Uri featureUrl, 
                string title, 
                LayerData parent
            ) {
                FeatureUrl = featureUrl ?? throw new ArgumentNullException(nameof(featureUrl));
                Title = !string.IsNullOrWhiteSpace(title) ? title
                    : throw new ArgumentException("Title is required.", nameof(title));
                Parent = parent ?? throw new ArgumentNullException(nameof(parent));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args)
        {
            if (args is not Args wfsArgs)
                throw new ArgumentException($"Expected {nameof(Args)} for preset wfs-layer.");

            var color = LayerColor.Random();

            var styling = new Symbolizer();
            styling.SetFillColor(color);
            styling.SetStrokeColor(color);

            var uri = AssetUriFactory.CreateRemoteAssetUri(wfsArgs.FeatureUrl.ToString());

            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(wfsArgs.Title)
                .ChildOf(wfsArgs.Parent)
                .WithColor(color)
                .SetDefaultStyling(styling)
                .AddProperty(new LayerURLPropertyData(uri));
        }
    }
}