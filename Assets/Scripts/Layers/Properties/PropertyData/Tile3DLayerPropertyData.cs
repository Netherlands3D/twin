using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [Serializable]
    public class Tile3DLayerPropertyData : LayerPropertyData
    {
        [SerializeField, JsonProperty] private string url;

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