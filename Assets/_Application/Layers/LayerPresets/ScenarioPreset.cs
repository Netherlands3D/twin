using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    [LayerPreset("scenario-layer")]
    public sealed class ScenarioPreset : ILayerPreset<ScenarioPreset.Args>
    { 
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
                .NamedAs(args.Name)
                .AddProperty(new ScenarioPropertyData());
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}