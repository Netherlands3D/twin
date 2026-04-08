using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Building")]
    public class BuildingPropertyData : LayerPropertyData
    {
        [JsonIgnore] private Dictionary<string, Coordinate> buildingIds = new();
        
        [JsonIgnore] public readonly UnityEvent<Dictionary<string, Coordinate>> OnIdsChanged = new();

        [JsonIgnore]
        public Dictionary<string, Coordinate> BuildingIds
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