using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Feature")]
    public class FeaturePropertyData : LayerPropertyData
    {
        [JsonIgnore] private Dictionary<string, FeatureData> featureIds = new();
        
        [JsonIgnore] public readonly UnityEvent<Dictionary<string, FeatureData>> OnIdsChanged = new();

        [JsonIgnore]
        public Dictionary<string, FeatureData> FeatureIds
        {
            get => featureIds;
            set
            {
                featureIds = value;
                OnIdsChanged.Invoke(featureIds);
            }
        }
        
        public struct FeatureData
        {
            public BoundingBox BoundingBox;
            public Dictionary<string, object> Properties;
        }
    }
}