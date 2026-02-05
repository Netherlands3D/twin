using System;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "SensorPropertyData")]
    public class SensorPropertyData : LayerPropertyData
    {
        [DataMember] private float minValue;
        [DataMember] private float maxValue;
        [DataMember] private Color minColor;
        [DataMember] private Color maxColor;
        [DataMember] private DateTime startDate;
        [DataMember] private DateTime endDate;
        
        [JsonIgnore]
        public float MinValue
        {
            get => minValue;
            set
            {
                minValue = value;
                OnMinValueChanged.Invoke(value);
            } 
        }

        [JsonIgnore]
        public float MaxValue
        {
            get => maxValue;
            set
            {
                maxValue = value;
                OnMaxValueChanged.Invoke(value);
            }
        }

        [JsonIgnore]
        public Color MinColor
        {
            get => minColor;
            set
            {
                minColor = value;
                OnMinColorChanged.Invoke(value);
            }
        }

        [JsonIgnore]
        public Color MaxColor
        {
            get => maxColor;
            set
            {
                maxColor = value;
                OnMaxColorChanged.Invoke(value);
            }
        }
        
        [JsonIgnore]
        public DateTime StartDate
        {
            get => startDate;
            set
            {
                startDate = value;
                OnStartDateChanged.Invoke(value);
            }
        }

        [JsonIgnore]
        public DateTime EndDate
        {
            get => endDate;
            set
            {
                endDate = value;
                OnEndDateChanged.Invoke(value);
            }
        }

        [JsonIgnore] public readonly UnityEvent<float> OnMinValueChanged = new();
        [JsonIgnore] public readonly UnityEvent<float> OnMaxValueChanged = new();
        [JsonIgnore] public readonly UnityEvent<Color> OnMinColorChanged = new();
        [JsonIgnore] public readonly UnityEvent<Color> OnMaxColorChanged = new();
        [JsonIgnore] public readonly UnityEvent<DateTime> OnStartDateChanged = new();
        [JsonIgnore] public readonly UnityEvent<DateTime> OnEndDateChanged = new();
        [JsonIgnore] public readonly UnityEvent OnResetValues = new();
        
        [JsonConstructor]
        public SensorPropertyData(float defaultMin, float defaultMax, Color defaultMinColor, Color defaultMaxColor, DateTime defaultStartDate, DateTime defaultEndDate)
        {
            minValue = defaultMin;
            maxValue = defaultMax;
            minColor = defaultMinColor;
            maxColor = defaultMaxColor;
            startDate = defaultStartDate;
            endDate = defaultEndDate;
        }
    }
}
