using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Url")]
    public class LayerURLPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri url;

        [JsonIgnore] public readonly UnityEvent<Uri> OnDataChanged = new();

        [JsonIgnore]
        public Uri Data
        {
            get => url;
            set
            {
                url = value;
                OnDataChanged.Invoke(value);
            }
        }

        public IEnumerable<LayerAsset> GetAssets()
        {
            return new List<LayerAsset>()
            {
                new (this, url != null ? url : null)
            };
        }
    }
}
