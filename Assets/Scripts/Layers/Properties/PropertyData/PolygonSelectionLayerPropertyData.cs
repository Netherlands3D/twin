using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "PolygonSelection")]
    public class PolygonSelectionLayerPropertyData : LayerPropertyData
    {
        [DataMember] private float lineWidth = 10f;
        [DataMember] private float extrusionHeight = 10f;
        
        [JsonIgnore] public readonly UnityEvent<float> OnLineWidthChanged = new();
        [JsonIgnore] public readonly UnityEvent<float> OnExtrusionHeightChanged = new();

        [JsonIgnore]
        public float LineWidth
        {
            get => lineWidth;
            set
            {
                lineWidth = value;
                OnLineWidthChanged.Invoke(lineWidth);
            }
        }
        
        [JsonIgnore]
        public float ExtrusionHeight
        {
            get => extrusionHeight;
            set
            {
                extrusionHeight = value;
                OnExtrusionHeightChanged.Invoke(extrusionHeight);
            }
        }
    }
}
