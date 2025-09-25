using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Functionalities.Wms.LayerPresets
{
    [LayerPreset("wms")]
    public sealed class Wms : ILayerPreset
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
            if (args is not Args wmsArgs)
            {
                throw new ArgumentException($"Expected {nameof(Args)} for preset wms.");
            }

            var uri = AssetUriFactory.CreateRemoteAssetUri(wmsArgs.CapabilitiesUrl.ToString());

            return builder
                .OfType("url")
                .AddProperty(new LayerURLPropertyData(uri));
        }
    }
}