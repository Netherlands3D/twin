using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Annotation")]
    public class AnnotationPropertyData : LayerPropertyData
    {
        [DataMember] private string annotationText;
        [DataMember] private string title;
        [DataMember] private string imageUrl;
        [DataMember] private string imageCaption;
        [DataMember] private bool readOnly;

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

        [JsonIgnore]
        public string Title
        {
            get => title;
            set => title = value;
        }

        [JsonIgnore]
        public string ImageUrl
        {
            get => imageUrl;
            set => imageUrl = value;
        }

        [JsonIgnore]
        public string ImageCaption
        {
            get => imageCaption;
            set => imageCaption = value;
        }
        
        [JsonIgnore]
        public bool ReadOnly
        {
            get => readOnly;
            set => readOnly = value;
        }
        

        [JsonConstructor]
        public AnnotationPropertyData(
            string annotationText,
            string title = null,
            string imageUrl = null,
            string imageCaption = null,
            bool readOnly = false)
        {
            this.annotationText = annotationText;
            this.title = title;
            this.imageUrl = imageUrl;
            this.imageCaption = imageCaption;
            this.readOnly = readOnly;
        }
    }
}
