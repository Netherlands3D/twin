using System;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [Serializable]
    public class Tile3DLayerPropertyData : LayerPropertyData
    {
        [JsonProperty] private Uri url;

        [JsonIgnore]
        public Uri Url
        {
            get => url;
            set
            {
                url = value;
                OnUrlChanged.Invoke(value);
            }
        }

        [JsonIgnore] public readonly UnityEvent<Uri> OnUrlChanged = new();

        [JsonConstructor]
        public Tile3DLayerPropertyData(Uri url)
        {
            this.url = url;
        }
    }
}