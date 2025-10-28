using System;
using JetBrains.Annotations;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;

namespace Netherlands3D.Functionalities.OBJImporter.LayerPresets
{
    [LayerPreset("obj")]
    public sealed class ObjPreset : ILayerPreset<ObjPreset.Args>
    {
        private const string PrefabIdentifier = "34882a73ff6122243a0e3e9811473e20";

        public sealed class Args : LayerPresetArgs<ObjPreset>
        {
            public string Name { get; }
            public Uri Url { get; }
            public Uri MtlUrl { get; set; }

            public Args(string name, Uri objFile, [CanBeNull] Uri mtlFile = null) 
            {
                Name = name;
                Url = objFile ?? throw new ArgumentNullException(nameof(objFile));
                MtlUrl = mtlFile;
            }
        }

        public ILayerBuilder Apply(ILayerBuilder builder, Args args)
        {
            var layerPropertyData = new OBJPropertyData { ObjFile = args.Url };
            if (args.MtlUrl != null)
            {
                layerPropertyData.MtlFile = args.MtlUrl;
            }

            return builder
                .NamedAs(args.Name)
                .OfType(PrefabIdentifier)
                .AddProperty(layerPropertyData);
        }

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args) => Apply(builder, (Args)args);
    }
}