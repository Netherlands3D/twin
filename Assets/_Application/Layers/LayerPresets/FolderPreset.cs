using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    [LayerPreset("folder-layer")]
    public sealed class FolderPreset : ILayerPreset<FolderPreset.Args>
    {
        public sealed class Args : LayerPresetArgs<FolderPreset>
        {
            public string Name;
            public bool IsScenario;

            public Args(string name) 
            {
                Name = name;
            }
            
            public Args(string name, bool isScenario) 
            {
                Name = name;
                IsScenario = isScenario;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            var propertyData = new FolderPropertyData(args.IsScenario);
            return builder
                .NamedAs(args.Name)
                .AddProperty(propertyData);
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}