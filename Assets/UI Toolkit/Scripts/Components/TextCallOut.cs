using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TextCallOut : VisualElement
    {
        private Label label;
        public TextCallOut()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            label = this.Q<Label>();
        }

        [UxmlAttribute("text")]
        public string Text
        {
            get => label.text;
            set => label.text = value;
        }
    }
}