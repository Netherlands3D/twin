using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [Serializable]
    public class WindmillPropertyData : LayerPropertyData
    {
        [SerializeField, JsonProperty] private float axisHeight = 120f;
        [SerializeField, JsonProperty] private float rotorDiameter = 120f;
        
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
