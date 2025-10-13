using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;

namespace Netherlands3D.Functionalities.CityJSON.LayerPresets
{
    [LayerPreset("cityjson")]
    public sealed class CityJSON : ILayerPreset
    {
        private const string PrefabIdentifier = "72d5fc36c601a427dac350e2b1146f0f";

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
            if (args is not Args cityJsonArgs)
            {
                throw new ArgumentException($"Expected {nameof(Args)} for preset cityjson.");
            }

            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(cityJsonArgs.Name)
                .AddProperty(new CityJSONPropertyData { CityJsonFile = cityJsonArgs.Url });
        }
    }
}