using System;
using JetBrains.Annotations;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerPresets;

namespace Netherlands3D.Functionalities.OBJImporter.LayerPresets
{
    [LayerPreset("obj")]
    public sealed class ObjPreset : ILayerPreset
    {
        private const string PrefabIdentifier = "34882a73ff6122243a0e3e9811473e20";

        public sealed class Args : LayerPresetArgs
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

        public ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args)
        {
            if (args is not Args objArgs)
            {
                throw new ArgumentException($"Expected {nameof(Args)} for preset obj.");
            }

            var layerPropertyData = new OBJPropertyData { ObjFile = objArgs.Url };
            if (objArgs.MtlUrl != null)
            {
                layerPropertyData.MtlFile = objArgs.MtlUrl;
            }

            return builder
                .NamedAs(objArgs.Name)
                .OfType(PrefabIdentifier)
                .AddProperty(layerPropertyData);
        }
    }
}