using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;

namespace Netherlands3D.Functionalities.OGC3DTiles.LayerPresets
{
    [LayerPreset("3d-tiles")]
    public sealed class OGC3DTilesPreset : ILayerPreset<OGC3DTilesPreset.Args>
    {
        private const string PrefabIdentifier = "395dd4e52bd3b42cfb24f183f3839bba";

        public sealed class Args : LayerPresetArgs<OGC3DTilesPreset>
        {
            public Uri Url { get; }

            public Args(Uri url)
            {
                Url = url;
            }

            public Args(string url) : this(new Uri(url))
            {
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder
                .NamedAs(args.Url.ToString())
                .OfType(PrefabIdentifier)
                .AddProperty(new Tile3DLayerPropertyData(args.Url));
        }
        
        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}