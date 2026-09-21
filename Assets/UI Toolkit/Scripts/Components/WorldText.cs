using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class WorldText : VisualElement
    {
        public EditableNameField NameField => nameField;
        public string Text => nameField.value;
        
        private VisualElement textContainer;
        private EditableNameField nameField;
        private Label placeholder;
        private Icon position;
        private VisualElement background;
        
        public enum SnappingSide { Left, Right, Above }
        private SnappingSide snappingSide = SnappingSide.Above;
        private float labelOffsetToPosition = 0;
        private string currentText;
        private bool isReadOnly = false;
        

        public WorldText()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            textContainer = this.Q<VisualElement>("TextContainer");
            nameField = this.Q<EditableNameField>();
            placeholder = this.Q<Label>("Placeholder");
            position = this.Q<Icon>("Position");
            position.pickingMode = PickingMode.Ignore;
            background = this.Q<VisualElement>("Background");
            
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            nameField.RegisterValueChangedCallback(OnNameChanged);
            
            nameField.ScrollingTextEnabled = false;
        }
        
        public WorldText(string text) : this()
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

        public void SetSnappingSide(SnappingSide snappingSide)
        {
            this.snappingSide = snappingSide;
        }

        public void SetLabelOffset(float offset)
        {
            labelOffsetToPosition = offset;
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
        }
    }
}
