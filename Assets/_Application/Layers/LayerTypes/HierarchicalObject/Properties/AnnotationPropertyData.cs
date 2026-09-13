using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Annotation")]
    public class AnnotationPropertyData : LayerPropertyData
    {
        [DataMember] private string annotationText;
        [DataMember] private string imagePath;
        [DataMember] private string imagePreviewPath;
        [DataMember] private string imageCaption;

        [JsonIgnore] public readonly UnityEvent<string> OnAnnotationTextChanged = new();
        [JsonIgnore] public readonly UnityEvent<string> OnImagePathChanged = new();
        [JsonIgnore] public readonly UnityEvent<string> OnImagePreviewPathChanged = new();
        [JsonIgnore] public readonly UnityEvent<string> OnImageCaptionChanged = new();

        [JsonIgnore]
        public string AnnotationText
        {
            get => annotationText;
            set
            {
                annotationText = value ?? "";
                OnAnnotationTextChanged.Invoke(annotationText);
            }
        }

        [JsonIgnore]
        public string ImagePath
        {
            get => imagePath;
            set
            {
                imagePath = value ?? "";
                OnImagePathChanged.Invoke(imagePath);

                if (string.IsNullOrWhiteSpace(imagePreviewPath))
                    OnImagePreviewPathChanged.Invoke(ImagePreviewPath);
            }
        }

        [JsonIgnore]
        public string ImagePreviewPath
        {
            get => string.IsNullOrWhiteSpace(imagePreviewPath) ? imagePath : imagePreviewPath;
            set
            {
                imagePreviewPath = value ?? "";
                OnImagePreviewPathChanged.Invoke(ImagePreviewPath);
            }
        }

        [JsonIgnore]
        public string ImageCaption
        {
            get => imageCaption;
            set
            {
                imageCaption = value ?? "";
                OnImageCaptionChanged.Invoke(imageCaption);
            }
        }

        [JsonConstructor]
        public AnnotationPropertyData(string annotationText, string imagePath, string imagePreviewPath, string imageCaption)
        {
            this.annotationText = annotationText ?? "";
            this.imagePath = imagePath ?? "";
            this.imagePreviewPath = imagePreviewPath ?? "";
            this.imageCaption = imageCaption ?? "";
        }

        public AnnotationPropertyData(string annotationText, string imagePath, string imageCaption) : this(annotationText, imagePath, imagePath, imageCaption)
        {
        }

        public AnnotationPropertyData(string annotationText) : this(annotationText, "", "")
        {
        }
    }
}
