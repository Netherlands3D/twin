using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Functionalities.NetCDF.LayerPresets
{
    [LayerPreset("netcdf-layer")]
    public sealed class NetCDFLayerPreset : ILayerPreset<NetCDFLayerPreset.Args>
    {
        private const string PrefabIdentifier = "8e3a4a527026893879718f3776721a71";

        public sealed class Args : LayerPresetArgs<NetCDFLayerPreset>
        {
            public string Title { get; }
            public Uri Url { get; }

            public Args(Uri url , string title) 
            {
                Title = title;
                Url = url ?? throw new ArgumentNullException(nameof(url));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder
                .OfType(PrefabIdentifier)
                .NamedAs(args.Title)
                .AddProperty(new LayerURLPropertyData(args.Url));
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}