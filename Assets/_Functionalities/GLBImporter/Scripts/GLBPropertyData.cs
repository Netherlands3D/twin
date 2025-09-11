using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.GLBImporter
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Glb")]
    public class GLBPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri glbFile;

        [JsonIgnore] public readonly UnityEvent<Uri> GlbUriChanged = new();

        [JsonIgnore]
        public Uri GlbFile
        {
            get => glbFile;
            set
            {
                glbFile = value;
                GlbUriChanged.Invoke(value);
            }
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            var existingAssets = new List<LayerAsset>();

            if (glbFile != null)
            {
                existingAssets.Add(new(this, glbFile));
            }

            return existingAssets;
        }
    }
}