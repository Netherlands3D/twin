using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Functionalities.Wms.LayerPresets
{
    [LayerPreset("wms-layer")]
    public sealed class WmsLayerPreset : ILayerPreset<WmsLayerPreset.Args>
    {
        private const string PrefabIdentifier = "7ddb78a6acbf44d4e84910b5684042b7";

        public sealed class Args : LayerPresetArgs<WmsLayerPreset>
        {
            public Uri Url { get; }
            public MapFilters Filters { get; }
            public LayerData Parent { get; }
            public bool DefaultEnabled { get; }

            public Args(
                Uri uri, 
                MapFilters filters, 
                LayerData parent, 
                bool defaultEnabled = false
            ) {
                Url = uri ?? throw new ArgumentNullException(nameof(uri));
                Filters = filters;
                Parent = parent;
                DefaultEnabled = defaultEnabled;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(args.Filters.name)
                .ChildOf(args.Parent)
                .AddProperty(new LayerURLPropertyData(args.Filters.ToUrlBasedOn(args.Url)))
                .WhenBuilt(layerData => layerData.ActiveSelf = args.DefaultEnabled);
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}