using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Transform")]
    public class TimelineLayerPropertyData : LayerPropertyData
    {
        [DataMember] private DateTime? buildStart;
        [DataMember] private DateTime? buildEnd;
        [DataMember] private DateTime? demolishStart;
        [DataMember] private DateTime? demolishEnd;
        
        [JsonIgnore] public readonly UnityEvent<DateTime?> OnBuildStartChanged = new();
        [JsonIgnore] public readonly UnityEvent<DateTime?> OnBuildEndChanged = new();
        [JsonIgnore] public readonly UnityEvent<DateTime?> OnDemolishStartChanged = new();
        [JsonIgnore] public readonly UnityEvent<DateTime?> OnDemolishEndChanged = new();
        
        [JsonIgnore]
        public DateTime? BuildStart
        {
            get => buildStart;
            set
            {
                buildStart = value;
                OnBuildStartChanged.Invoke(value);
            }
        }
        
        [JsonIgnore]
        public DateTime? BuildEnd
        {
            get => buildEnd;
            set
            {
                buildEnd = value;
                OnBuildEndChanged.Invoke(value);
            }
        }
        
        [JsonIgnore]
        public DateTime? DemolishStart
        {
            get => demolishStart;
            set
            {
                demolishStart = value;
                OnDemolishStartChanged.Invoke(value);
            }
        }
        
        [JsonIgnore]
        public DateTime? DemolishEnd
        {
            get => demolishEnd;
            set
            {
                demolishEnd = value;
                OnDemolishEndChanged.Invoke(value);
            }
        }
        
        [JsonConstructor]
        public TimelineLayerPropertyData(DateTime? buildStart, DateTime? buildEnd, DateTime? demolishStart, DateTime? demolishEnd)
        {
            this.buildStart = buildStart;
            this.buildEnd = buildEnd;
            this.demolishStart = demolishStart;
            this.demolishEnd = demolishEnd;
        }
    }
}