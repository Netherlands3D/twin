using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Functionalities.Wfs.LayerPresets
{
    [LayerPreset("wfs")]
    public sealed class Wfs : ILayerPreset
    {
        public sealed class Args : LayerPresetArgs
        {
            public Uri CapabilitiesUrl { get; }

            public Args(Uri capabilitiesUrl) 
            {
                CapabilitiesUrl = capabilitiesUrl ?? throw new ArgumentNullException(nameof(capabilitiesUrl));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args)
        {
            if (args is not Args wfsArgs)
            {
                throw new ArgumentException($"Expected {nameof(Args)} for preset wfs.");
            }

            var uri = AssetUriFactory.CreateRemoteAssetUri(wfsArgs.CapabilitiesUrl.ToString());

            return builder
                .OfType("url")
                .AddProperty(new LayerURLPropertyData(uri));
        }
    }
}