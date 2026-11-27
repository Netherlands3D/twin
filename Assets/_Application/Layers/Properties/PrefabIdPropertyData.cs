using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "PrefabId")]
    public class PrefabIdPropertyData : LayerPropertyData
    {
        [DataMember] private string prefabId;

        [JsonIgnore] public readonly UnityEvent<string> OnPrefabIdChanged = new();

        [JsonIgnore]
        public string PrefabId
        {
            get => prefabId;
            set
            {
                prefabId = value;
                OnPrefabIdChanged.Invoke(value);
            }
        }

        [JsonConstructor]
        public PrefabIdPropertyData(string prefabId)
        {
            this.prefabId = prefabId;
        }
    }
}
