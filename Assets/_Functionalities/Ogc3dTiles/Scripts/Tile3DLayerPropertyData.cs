using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "3DTiles")]
    public class Tile3DLayerPropertyData : LayerPropertyData
    {
        [DataMember] private string url;

        [JsonIgnore]
        public string Url
        {
            get => url;
            set
            {
                url = value;
                OnUrlChanged.Invoke(value);
            }
        }

        [JsonIgnore] public readonly UnityEvent<string> OnUrlChanged = new();
        
        [JsonConstructor]
        public Tile3DLayerPropertyData(string url)
        {
            this.url = url;
        }
    }
}