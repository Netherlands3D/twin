using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "ConnectableHierarchicalLayer")]
    public class ConnectableHierarchicalLayerPropertyData : LayerPropertyData
    {
        [DataMember] private LayerData target;
        
        [JsonIgnore]
        public LayerData Target
        {
            get => target;
            set
            {
                target = value;
                OnTargetChanged.Invoke(target);
            }
        }

        [JsonIgnore] public readonly UnityEvent<LayerData> OnTargetChanged = new();
    }
}