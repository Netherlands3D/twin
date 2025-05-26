using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Annotation")]
    public class AnnotationPropertyData : TransformLayerPropertyData
    {
        [DataMember] private string annotationText;

        [JsonIgnore] public readonly UnityEvent<string> OnAnnotationTextChanged = new();

        [JsonIgnore]
        public string AnnotationText
        {
            get => annotationText;
            set
            {
                annotationText = value;
                OnAnnotationTextChanged.Invoke(value);
            }
        }
        
        [JsonConstructor]
        public AnnotationPropertyData(Coordinate position, Vector3 eulerRotation, Vector3 localScale, string annotationText) : base(position, eulerRotation, localScale)
        {
            this.annotationText = annotationText;
        }

    }
}
