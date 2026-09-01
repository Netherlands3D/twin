using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class WorldText : VisualElement
    {
        private EditableNameField nameField;
        private VisualElement position;
        
        public enum SnappingSide { Left, Right, Above }
        private SnappingSide snappingSide = SnappingSide.Above;

        private float labelOffsetToPosition = 0;
        
        
        public WorldText()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            nameField = this.Q<EditableNameField>();
            position = this.Q<VisualElement>("Position");
            
            RegisterCallback<GeometryChangedEvent>(UpdateSnapping);
        }

        public void SetText(string text)
        {
            nameField.value = text;
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
            float pivotX = -(position.resolvedStyle.width * 0.5f);
            float pivotY = (position.resolvedStyle.height * 0.5f);
            switch (snappingSide)
            {
                case SnappingSide.Left:
                {
                    float offsetX = -nameField.resolvedStyle.width - pivotX - labelOffsetToPosition;
                    float offsetY = pivotY - (nameField.resolvedStyle.height * 0.5f);
                    nameField.style.translate = new Translate(offsetX, offsetY, 0);
                    break;
                }
                case SnappingSide.Right:
                {
                    float offsetX = - pivotX + labelOffsetToPosition;
                    float offsetY = pivotY - (nameField.resolvedStyle.height * 0.5f);
                    nameField.style.translate = new Translate(offsetX, offsetY, 0);
                    break;
                }
                case SnappingSide.Above:
                {
                    float offsetX = -nameField.resolvedStyle.width * 0.5f - pivotX;
                    float offsetY =  pivotY - labelOffsetToPosition;
                    nameField.style.translate = new Translate(offsetX, offsetY, 0);
                    break;
                }
            }
        }
    }
}
