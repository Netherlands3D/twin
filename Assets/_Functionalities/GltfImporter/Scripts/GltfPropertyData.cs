using System.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Functionalities.GltfImporter
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Gltf")]
    public class GltfPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri uri;

        [JsonIgnore] public readonly UnityEvent<Uri> OnUriChanged = new();

        [JsonIgnore]
        public Uri Uri
        {
            get => uri;
            set
            {
                uri = value;
                OnUriChanged.Invoke(value);
            }
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            return new List<LayerAsset>
            {
                new (this, uri)
            };
        }
    }
}
