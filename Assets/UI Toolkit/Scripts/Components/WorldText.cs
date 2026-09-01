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

        private void UpdateSnapping(GeometryChangedEvent evt)
        {
            switch (snappingSide)
            {
                case SnappingSide.Left:
                {
                    float offsetX = -(nameField.resolvedStyle.width * 0.5f) - (position.resolvedStyle.width * 0.5f);
                    float offsetY = (nameField.resolvedStyle.height * 0.5f) + (position.resolvedStyle.height * 0.5f);
                    nameField.style.translate = new Translate(offsetX, offsetY, 0);
                    break;
                }
                case SnappingSide.Right:
                {
                    nameField.style.transformOrigin = new TransformOrigin(0.5f, 0);
                    break;
                }
                case SnappingSide.Above:
                {
                    nameField.style.transformOrigin = new TransformOrigin(0f, 0.5f);
                    break;
                }
            }
        }
    }
}
