using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;

namespace Netherlands3D.Functionalities.GeoTiff.LayerPresets
{
    [LayerPreset("geo-tiff")]
    public sealed class GeoTiffPreset : ILayerPreset<GeoTiffPreset.Args>
    {
        public const string PrefabIdentifier = "7609cfac99ea443a97b73b95d6e77109";

        public sealed class Args : LayerPresetArgs<GeoTiffPreset>
        {
            public string Name { get; }
            public Uri Url { get; }

            public Args(string name, Uri url)
            {
                Name = name;
                Url = url ?? throw new ArgumentNullException(nameof(url));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder.NamedAs(args.Name).OfType(PrefabIdentifier).AddProperty(new GeoTiffPropertyData
            {
                GeoTiffFile = args.Url
            });
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}
