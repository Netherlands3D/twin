using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    [LayerPreset("dashboard-csv")]
    public sealed class DashboardCsvPreset : ILayerPreset<DashboardCsvPreset.Args>
    {
        public sealed class Args : LayerPresetArgs<DashboardCsvPreset>
        {
            public string Name { get; }
            public DashboardCsvPropertyData PropertyData { get; }

            public Args(string name, DashboardCsvPropertyData propertyData)
            {
                Name = name;
                PropertyData = propertyData;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args) => builder.NamedAs(args.Name).AddProperty(args.PropertyData);

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}
