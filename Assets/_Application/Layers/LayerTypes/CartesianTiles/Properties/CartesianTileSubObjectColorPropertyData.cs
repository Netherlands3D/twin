using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "CartesianTileSubObjectColor")]
    public class CartesianTileSubObjectColorPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        [DataMember] private Uri data;
        
        [JsonIgnore] public readonly UnityEvent<Uri> OnDataChanged = new();
        internal event Action ColorsChanged;

        [JsonIgnore] internal IReadOnlyDictionary<string, Color> Colors { get; private set; }
        
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

        internal void LoadFromProjectAsset()
        {
            Colors = new CartesianTileSubObjectColorCsv(Data).Read();
            ColorsChanged?.Invoke();
        }
    }
}
