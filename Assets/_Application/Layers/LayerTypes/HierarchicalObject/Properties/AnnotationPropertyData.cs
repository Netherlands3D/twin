using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Annotation")]
    public class AnnotationPropertyData : LayerPropertyData
    {
        [DataMember] private string annotationText;

        [JsonIgnore] public readonly UnityEvent<string> OnDataChanged = new();

        [JsonIgnore]
        public string Data
        {
            get => annotationText;
            set
            {
                annotationText = value;
                OnDataChanged.Invoke(value);
            }
        }
        
        [JsonConstructor]
        public AnnotationPropertyData(string annotationText)
        {
            this.annotationText = annotationText;
        }
    }
}
