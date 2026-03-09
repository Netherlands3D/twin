using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Header : VisualElement
    {
        // Query and cache icon component
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>("Icon");

        // Query and cache label component
        private Label labelField;
        private Label Label => labelField ??= this.Q<Label>("Label");

        // Pass-throughs
        [UxmlAttribute("icon")]
        public IconImage Image
        {
            get => Icon.Image;
            set => Icon.Image = value;
        }

        [UxmlAttribute("text")]
        public string LabelText
        {
            get => Label.text;
            set => Label.text = value;
        }

        public Header()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}
