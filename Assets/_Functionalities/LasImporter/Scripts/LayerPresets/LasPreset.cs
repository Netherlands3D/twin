using System;
using JetBrains.Annotations;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;

namespace Netherlands3D.Functionalities.LASImporter.LayerPresets
{
    [LayerPreset("las")]
    public sealed class LasPreset : ILayerPreset<LasPreset.Args>
    {
        // change this to the guid/id of the prefab you make for the LAS layer
        private const string PrefabIdentifier = "34882a73ff6122243a0e3e9811473e20";

        public sealed class Args : LayerPresetArgs<LasPreset>
        {
            public string Name { get; }
            public Uri Url { get; }

            public Args(string name, Uri url)
            {
                Name = name;
                Url = url;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            var propertyData = new LASPropertyData
            {
                LasFile = args.Url
            };

            return builder
                .NamedAs(args.Name)
                .OfType(PrefabIdentifier)
                .AddProperty(propertyData);
        }

        ILayerBuilder ILayerPreset.Apply(ILayerBuilder builder, LayerPresetArgs args)
            => Apply(builder, (Args)args);
    }
}
