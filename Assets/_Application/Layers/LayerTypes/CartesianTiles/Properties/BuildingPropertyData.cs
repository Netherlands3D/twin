using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Building")]
    public class BuildingPropertyData : LayerPropertyData
    {
        [JsonIgnore] private List<string> buildingIds = new();
        
        [JsonIgnore] public readonly UnityEvent<List<string>> OnIdsChanged = new();

        [JsonIgnore]
        public List<string> BuildingIds
        {
            get => buildingIds;
            set
            {
                buildingIds = value;
                OnIdsChanged.Invoke(buildingIds);
            }
        }
    }
}