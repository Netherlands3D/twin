using System;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Functionalities.LayerPresets
{
    [LayerPreset("asset-bundle-layer")]
    public sealed class AssetBundleLayerPreset : ILayerPreset<AssetBundleLayerPreset.Args>
    {
        private const string PrefabIdentifier = "96d778a591e307e48b3f896321843660";

        public sealed class Args : LayerPresetArgs<AssetBundleLayerPreset>
        {
            public Uri AssetBundleUrl { get; }
            public string Title { get; }

            public Args(Uri assetBundleUrl, string title)
            {
                AssetBundleUrl = assetBundleUrl ?? throw new ArgumentNullException(nameof(assetBundleUrl));
                Title = title ?? throw new ArgumentNullException(nameof(title));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(args.Title)
                .AddProperty(new LayerURLPropertyData(args.AssetBundleUrl));
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}