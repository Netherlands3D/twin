using System;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    [LayerPreset("scenario-layer")]
    public sealed class ScenarioPreset : ILayerPreset<ScenarioPreset.Args>
    {
        public const string PrefabIdentifier = "scenario_folder";

        public sealed class Args : LayerPresetArgs<ScenarioPreset>
        {
            public string Name;

            public Args(
                string name
            ) {
                Name = name;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(args.Name);
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}