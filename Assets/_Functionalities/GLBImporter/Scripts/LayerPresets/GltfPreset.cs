using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Functionalities.GLBImporter.LayerPresets
{
    [LayerPreset("gltf")]
    public sealed class GltfPreset : ILayerPreset<GltfPreset.Args>
    {
        private const string PrefabIdentifier = "9c30c9cc071ed4343b05fb7ded7859d2";

        public sealed class Args : LayerPresetArgs<GltfPreset>
        {
            public string Name { get; }
            public Uri Url { get; }

            public Args(string name, Uri gltfFile) 
            {
                Name = name;
                Url = gltfFile ?? throw new ArgumentNullException(nameof(gltfFile));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder
                .NamedAs(args.Name)
                .OfType(PrefabIdentifier)
                .AddProperty(new GLBPropertyData
                {
                    GlbFile = AssetUriFactory.CreateProjectAssetUri(args.Url.ToString())
                });
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}