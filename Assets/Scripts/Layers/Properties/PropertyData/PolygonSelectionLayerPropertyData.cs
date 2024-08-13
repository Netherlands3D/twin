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
        [JsonIgnore] public readonly UnityEvent<float> OnLineWidthChanged = new();

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
    }
}
