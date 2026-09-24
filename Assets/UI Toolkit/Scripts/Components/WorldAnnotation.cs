using System;
using System.Collections;
using System.Threading.Tasks;
using KindMen.Uxios;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.Web;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class WorldAnnotation : VisualElement
    {
        public EditableNameField NameField => nameField;
        public string Text => nameField.value;
        
        private VisualElement textContainer;
        private EditableNameField nameField;
        private Label placeholder;
        private Icon position;
        private VisualElement background;
        
        private const int MaxImageDimension = 512;
        private VisualElement image;
        
        public enum SnappingSide { Left, Right, Above }
        private SnappingSide snappingSide = SnappingSide.Above;
        private float labelOffsetToPosition = 0;
        private string currentText;
        private bool isReadOnly = false;
        
        private bool isCoordinate = false;
        [UxmlAttribute("coordinate")]
        public bool IsCoordinate
        {
            get => isCoordinate;
            set 
            {
                isCoordinate = value;
                SetReadOnly(isCoordinate);
                if(isCoordinate)
                    nameField.Label.AddToClassList("editable-name-field-label-coordinates");
                else
                    nameField.Label.RemoveFromClassList("editable-name-field-label-coordinates");
            }
        }

        public WorldAnnotation()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            textContainer = this.Q<VisualElement>("TextContainer");
            nameField = this.Q<EditableNameField>();
            placeholder = this.Q<Label>("Placeholder");
            position = this.Q<Icon>("Position");
            position.pickingMode = PickingMode.Ignore;
            background = this.Q<VisualElement>("Background");
            image =  this.Q<VisualElement>("Image");
            
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            nameField.RegisterValueChangedCallback(OnNameChanged);
            
            nameField.ScrollingTextEnabled = false;
        }
        
        public WorldAnnotation(string text) : this()
        {
            SetText(text);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateContainerSize();
            UpdateSnapping();
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            currentText = evt.newValue;
            UpdatePlaceholder();
            UpdateContainerSize();
        }

        private void UpdatePlaceholder()
        {
            bool isEmpty = string.IsNullOrEmpty(currentText);
            placeholder.EnableInClassList(UtilityClassConstants.HIDDEN, !isEmpty);
        }

        public void SetReadOnly(bool isReadOnly)
        {
            this.isReadOnly = isReadOnly;
            nameField.IsEditable = !isReadOnly;
        }
        
        public void SetText(string text)
        {
            currentText = text;
            nameField.value = text;
            UpdatePlaceholder();
        }

        public async Task SetImage(string url)
        {
            var imageUrl = AddImageSizeParameters(url);
            var texture = await DownloadTextureAsync(imageUrl);

            if (texture == null)
                return;

            if (texture.width > MaxImageDimension || texture.height > MaxImageDimension)
            {
                Debug.LogError($"image has too large resolution: {texture.width}x{texture.height}");
                return;
            }

            image.style.width = texture.width;
            image.style.height = texture.height;
            image.style.backgroundImage = new StyleBackground(texture);

            UpdateSnapping();
        }

        private string AddImageSizeParameters(string url)
        {
            var uri = new Uri(url);
            var queryParameters = QueryString.Decode(uri.Query);
            var uriBuilder = new UriBuilder(uri);

            if (int.TryParse(queryParameters.Single("w"), out var sourceWidth) &&
                int.TryParse(queryParameters.Single("h"), out var sourceHeight))
            {
                int largestSide = Mathf.Max(sourceWidth, sourceHeight);

                if (largestSide > MaxImageDimension)
                {
                    float scale = MaxImageDimension / (float)largestSide;

                    int width = Mathf.Max(1, Mathf.RoundToInt(sourceWidth * scale));
                    int height = Mathf.Max(1, Mathf.RoundToInt(sourceHeight * scale));

                    uriBuilder.SetQueryParameter("w", width.ToString());
                    uriBuilder.SetQueryParameter("h", height.ToString());
                }
            }
            string format = queryParameters.Single("format")?.ToLowerInvariant();
            if (format is not "png" and not "jpg")
            {
                format = "jpg";
            }
            uriBuilder.SetQueryParameter("format", format);
            return uriBuilder.Uri.ToString();
        }

        private async Task<Texture2D> DownloadTextureAsync(string url)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            var operation = request.SendWebRequest();
            var tcs = new TaskCompletionSource<bool>();
            operation.completed += _ => tcs.TrySetResult(true);
            await tcs.Task;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download image: {request.error}");
                return null;
            }

            return DownloadHandlerTexture.GetContent(request);
        }

        public void SetSnappingSide(SnappingSide snappingSide)
        {
            this.snappingSide = snappingSide;
        }
        
        public void SetLabelOffset(float offset)
        {
            if(offset != labelOffsetToPosition)
            {
                labelOffsetToPosition = offset;
                UpdateSnapping();
            }
        }

        private void UpdateSnapping()
        {
            float offsetX = 0;
            float offsetY = 0;
            switch (snappingSide)
            {
                case SnappingSide.Left:
                {
                    offsetX = - textContainer.resolvedStyle.width  - labelOffsetToPosition;
                    offsetY = - (textContainer.resolvedStyle.height * 0.5f);
                    break;
                }
                case SnappingSide.Right:
                {
                    offsetX = labelOffsetToPosition;
                    offsetY = - (textContainer.resolvedStyle.height * 0.5f);
                    break;
                }
                case SnappingSide.Above:
                {
                    offsetX = -textContainer.resolvedStyle.width * 0.5f;
                    offsetY = - textContainer.resolvedStyle.height - labelOffsetToPosition ;
                    break;
                }
            }
            textContainer.style.translate = new Translate(offsetX, offsetY, 0);

            float backgroundBorderHeight = background.resolvedStyle.height - textContainer.resolvedStyle.height;
            image.style.translate = new Translate(
                -image.resolvedStyle.width * 0.5f,
                -image.resolvedStyle.height - labelOffsetToPosition - backgroundBorderHeight - textContainer.resolvedStyle.height * 0.5f,
                0
            );
        }
        
        public void SetAnnotationImage(Texture2D texture)
        {
            if (texture == null)
                return;

            int width = texture.width;
            image.style.width = width;

            var aspectRatio = (float)width / texture.height;
            image.style.height = width / aspectRatio;

            image.style.backgroundImage = new StyleBackground(texture);
        }
        
        private void UpdateContainerSize()
        {
            schedule.Execute(() =>
            {
                //get either the placeholder width when no text is present or the input field completed text or the being edited text width
                bool isEmpty = string.IsNullOrEmpty(currentText);
                float width = isEmpty ? placeholder.resolvedStyle.width : nameField.TextWidth;
                float height = isEmpty ? placeholder.resolvedStyle.height : nameField.TextHeight;

                textContainer.style.width = width;
                textContainer.style.height = height;
            });
        }
        
        public void SetColor(Color color)
        {
            textContainer.style.backgroundColor = color;
            position.style.unityBackgroundImageTintColor = color;
            background.style.backgroundColor = color;
            image.style.backgroundColor = color;
        }
    }
}
