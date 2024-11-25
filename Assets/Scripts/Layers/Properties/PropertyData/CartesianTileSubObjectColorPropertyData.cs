using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "CartesianTileSubObjectColor")]
    public class CartesianTileSubObjectColorPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri data;
        
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

        public IEnumerable<LayerAsset> GetAssets()
        {
            return new List<LayerAsset>()
            {
                new (this, data != null ? data : null)
            };
        }
    }
}
