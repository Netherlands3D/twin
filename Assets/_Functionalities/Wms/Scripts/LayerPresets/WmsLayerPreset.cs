using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Functionalities.Wms.LayerPresets
{
    [LayerPreset("wms-layer")]
    public sealed class WmsLayerPreset : ILayerPreset
    {
        private const string PrefabIdentifier = "7ddb78a6acbf44d4e84910b5684042b7";

        public sealed class Args : LayerPresetArgs
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

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args)
        {
            if (args is not Args wmsLayerArgs)
            {
                throw new ArgumentException($"Expected {nameof(Args)} for preset wms-layer.");
            }
            
            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(wmsLayerArgs.Filters.name)
                .ChildOf(wmsLayerArgs.Parent)
                .AddProperty(new LayerURLPropertyData(wmsLayerArgs.Filters.ToUrlBasedOn(wmsLayerArgs.Url)))
                .WhenBuilt(layerData => layerData.ActiveSelf = wmsLayerArgs.DefaultEnabled);
        }
    }
}