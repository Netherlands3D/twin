using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class AnnotationField : VisualElement
    {
        private EditableNameField field;
        
        public AnnotationField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            field = this.Q<EditableNameField>();
        }
    }
}