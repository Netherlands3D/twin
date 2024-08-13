using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class PolygonSelectionLayerPropertyData : LayerPropertyData
    {
        [SerializeField, JsonProperty] private float lineWidth = 10f;
        [SerializeField, JsonProperty] private float extrusionHeight = 10f;
        
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
