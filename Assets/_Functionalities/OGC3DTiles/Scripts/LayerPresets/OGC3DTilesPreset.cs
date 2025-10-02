using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using UnityEngine;

namespace Netherlands3D.Functionalities.OGC3DTiles.LayerPresets
{
    [LayerPreset("3d-tiles")]
    public sealed class OGC3DTilesPreset : ILayerPreset
    {
        private const string PrefabIdentifier = "395dd4e52bd3b42cfb24f183f3839bba";

        public sealed class Args : LayerPresetArgs
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

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args)
        {
            if (args is not Args tiles3dArgs)
                throw new ArgumentException($"Expected {nameof(Args)} for preset 3d-tiles.");

            return builder
                .OfType(PrefabIdentifier)
                .AddProperty(new Tile3DLayerPropertyData(tiles3dArgs.Url.ToString()));
        }
    }
}