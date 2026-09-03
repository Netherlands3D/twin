using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class WorldText : VisualElement
    {
        private VisualElement textContainer;
        private EditableNameField nameField;
        private Label placeholder;
        private VisualElement position;
        
        public enum SnappingSide { Left, Right, Above }
        private SnappingSide snappingSide = SnappingSide.Above;

        private float labelOffsetToPosition = 0;
        
        private IVisualElementScheduledItem clickTimer;
        [UxmlAttribute] public float ClickInterval { get; set; } = 0.5f;
        private bool waitingForClick = false;
        private string currentText;
        
        public WorldText()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            textContainer = this.Q<VisualElement>("TextContainer");
            nameField = this.Q<EditableNameField>();
            placeholder = this.Q<Label>("Placeholder");
            position = this.Q<VisualElement>("Position");
            
            RegisterCallback<GeometryChangedEvent>(UpdateSnapping);
            nameField.RegisterCallback<ClickEvent>(OnClick);
            nameField.RegisterValueChangedCallback(OnNameChanged);
            
            schedule.Execute(UpdateContainerSize).Every(30);
        }
        
        private void OnClick(ClickEvent evt)
        {
            Debug.Log("CLICKED");
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            currentText = evt.newValue;
            UpdatePlaceholder();
        }

        private void UpdatePlaceholder()
        {
            bool isEmpty = string.IsNullOrEmpty(currentText);
            placeholder.EnableInClassList(UtilityClassConstants.HIDDEN, !isEmpty);
        }
        
        public void SetText(string text)
        {
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

        private void UpdateSnapping(GeometryChangedEvent evt)
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
            bool isEmpty = string.IsNullOrEmpty(currentText);
            float width = isEmpty ? placeholder.resolvedStyle.width : nameField.TextWidth + 10;
            float height = isEmpty ? placeholder.resolvedStyle.height : nameField.resolvedStyle.height;

            textContainer.style.width = width;
            textContainer.style.height = height;
        }
    }
}
