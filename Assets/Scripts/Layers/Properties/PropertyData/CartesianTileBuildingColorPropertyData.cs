using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [Serializable]
    public class CartesianTileBuildingColorPropertyData : LayerPropertyData
    {
        [SerializeField, JsonProperty] private Uri data;
        
        [JsonIgnore] public readonly UnityEvent<Uri> OnDataChanged = new();
        
        [JsonIgnore]
        public Uri Data
        {
            get => data;
            set
            {
                data = value;
                OnDataChanged.Invoke(value);
            }
        }
    }
}
