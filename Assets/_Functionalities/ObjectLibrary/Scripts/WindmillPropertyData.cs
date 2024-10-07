using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Windmill")]
    public class WindmillPropertyData : LayerPropertyData
    {
        [DataMember] private float axisHeight = 120f;
        [DataMember] private float rotorDiameter = 120f;
        
        [JsonIgnore] public readonly UnityEvent<float> OnRotorDiameterChanged = new();
        [JsonIgnore] public readonly UnityEvent<float> OnAxisHeightChanged = new();
        
        [JsonIgnore]
        public float RotorDiameter
        {
            get => rotorDiameter;
            set
            {
                rotorDiameter = value;
                OnRotorDiameterChanged.Invoke(rotorDiameter);
            }
        }

        [JsonIgnore]
        public float AxisHeight
        {
            get => axisHeight;
            set
            {
                axisHeight = value;
                OnAxisHeightChanged.Invoke(axisHeight);
            }
        }
    }
}
