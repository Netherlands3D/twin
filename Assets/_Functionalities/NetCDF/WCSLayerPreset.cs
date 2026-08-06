using System;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Functionalities.NetCDF.LayerPresets
{
    [LayerPreset("wcs-layer")]
    public sealed class WCSLayerPreset : ILayerPreset<WCSLayerPreset.Args>
    {
        private const string PrefabIdentifier = "8e3a4a527026893879718f3776721a71";

        public sealed class Args : LayerPresetArgs<WCSLayerPreset>
        {
            public Uri Url { get; }
            public MapFilters Filters { get; }

            public Args(
                Uri uri, 
                MapFilters filters
            ) {
                Url = uri ?? throw new ArgumentNullException(nameof(uri));
                Filters = filters;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(args.Filters.name)
                .AddProperty(new LayerURLPropertyData(args.Filters.ToUrlBasedOnWCS(args.Url)));
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}