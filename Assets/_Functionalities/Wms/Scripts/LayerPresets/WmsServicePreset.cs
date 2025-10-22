using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Functionalities.Wms.LayerPresets
{
    [LayerPreset("wms")]
    public sealed class WmsServicePreset : ILayerPreset<WmsServicePreset.Args>
    {
        public sealed class Args : LayerPresetArgs<WmsServicePreset>
        {
            public Uri CapabilitiesUrl { get; }

            public Args(Uri capabilitiesUrl) 
            {
                CapabilitiesUrl = capabilitiesUrl ?? throw new ArgumentNullException(nameof(capabilitiesUrl));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            var uri = AssetUriFactory.CreateRemoteAssetUri(args.CapabilitiesUrl.ToString());

            return builder
                .OfType("url")
                .AddProperty(new LayerURLPropertyData(uri));
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}