using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;

namespace Netherlands3D.Functionalities.CityJSON.LayerPresets
{
    [LayerPreset("cityjson")]
    public sealed class CityJSONPreset : ILayerPreset<CityJSONPreset.Args>
    {
        private const string PrefabIdentifier = "72d5fc36c601a427dac350e2b1146f0f";

        public sealed class Args : LayerPresetArgs<CityJSONPreset>
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
            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(args.Name)
                .AddProperty(new CityJSONPropertyData { CityJsonFile = args.Url });
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}