using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Functionalities.GLBImporter.LayerPresets
{
    [LayerPreset("gltf")]
    public sealed class GltfPreset : ILayerPreset
    {
        private const string PrefabIdentifier = "9c30c9cc071ed4343b05fb7ded7859d2";

        public sealed class Args : LayerPresetArgs
        {
            public string Name { get; }
            public Uri Url { get; }

            public Args(string name, Uri gltfFile) 
            {
                Name = name;
                Url = gltfFile ?? throw new ArgumentNullException(nameof(gltfFile));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args)
        {
            if (args is not Args gltfArgs)
            {
                throw new ArgumentException($"Expected {nameof(Args)} for preset gltf.");
            }

            return builder
                .NamedAs(gltfArgs.Name)
                .OfType(PrefabIdentifier)
                .AddProperty(new GLBPropertyData
                {
                    GlbFile = AssetUriFactory.CreateProjectAssetUri(gltfArgs.Url.ToString())
                });
        }
    }
}