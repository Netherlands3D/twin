using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Feature")]
    public class FeaturePropertyData : LayerPropertyData
    {
        [JsonIgnore] private Dictionary<string, (Coordinate, Dictionary<string, object>)> featureIds = new();
        
        [JsonIgnore] public readonly UnityEvent<Dictionary<string, (Coordinate, Dictionary<string, object>)>> OnIdsChanged = new();

        [JsonIgnore]
        public Dictionary<string, (Coordinate, Dictionary<string, object>)> FeatureIds
        {
            get => featureIds;
            set
            {
                featureIds = value;
                OnIdsChanged.Invoke(featureIds);
            }
        }
    }
}