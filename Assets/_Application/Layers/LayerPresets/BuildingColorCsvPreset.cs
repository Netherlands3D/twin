using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    [LayerPreset("building-color-csv")]
    public sealed class BuildingColorCsvPreset : ILayerPreset<BuildingColorCsvPreset.Args>
    {
        public sealed class Args : LayerPresetArgs<BuildingColorCsvPreset>
        {
            public string Name { get; }
            public string PrefabIdentifier { get; }
            public CartesianTileSubObjectColorPropertyData PropertyData { get; }

            public Args(string name, string prefabIdentifier, CartesianTileSubObjectColorPropertyData propertyData)
            {
                Name = name;
                PrefabIdentifier = prefabIdentifier;
                PropertyData = propertyData;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args) => builder.OfType(args.PrefabIdentifier).NamedAs(args.Name).AddProperty(args.PropertyData);

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}
