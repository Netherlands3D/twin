using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    [LayerPreset("folder-layer")]
    public sealed class FolderPreset : ILayerPreset<FolderPreset.Args>
    {
        public sealed class Args : LayerPresetArgs<FolderPreset>
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
                .AddProperty(new FolderPropertyData());
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}