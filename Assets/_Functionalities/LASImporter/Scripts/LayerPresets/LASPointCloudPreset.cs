using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Projects;

namespace Netherlands3D.Functionalities.LASImporter.LayerPresets
{
    [LayerPreset("las-point-cloud")]
    public sealed class LASPointCloudPreset : ILayerPreset<LASPointCloudPreset.Args>
    {
        public const string PrefabIdentifier = "f54b3f1829b04b88b2f33f2b01e0d6a7";

        public sealed class Args : LayerPresetArgs<LASPointCloudPreset>
        {
            public string Name { get; }
            public Uri Url { get; }

            public Args(string name, Uri lasFile)
            {
                Name = name;
                Url = lasFile ?? throw new ArgumentNullException(nameof(lasFile));
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            return builder
                .NamedAs(args.Name)
                .OfType(PrefabIdentifier)
                .AddProperty(new LASPointCloudPropertyData
                {
                    LasFile = AssetUriFactory.CreateProjectAssetUri(args.Url.ToString())
                });
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}
