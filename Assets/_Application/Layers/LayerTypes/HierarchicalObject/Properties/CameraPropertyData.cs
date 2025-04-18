using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Camera")]
    public class CameraPropertyData : TransformLayerPropertyData
    {
        [DataMember] private bool orthographic;

        [JsonIgnore] public readonly UnityEvent<bool> OnOrthographicChanged = new();

        [JsonIgnore]
        public bool Orthographic
        {
            get => orthographic;
            set
            {
                orthographic = value;
                OnOrthographicChanged.Invoke(value);
            }
        }
        
        [JsonConstructor]
        public CameraPropertyData(Coordinate position, Vector3 eulerRotation, Vector3 localScale, bool orthographic) : base(position, eulerRotation, localScale)
        {
            this.orthographic = orthographic;
        }
    }
}
