using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToggleButton : Toggle
    {
        // Query and cache icon component
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>("Icon");

        // Query and cache label component
        private Label labelField;
        private Label Label => labelField ??= this.Q<Label>("Label");

        private readonly Button.Modifiers modifiers;
        [UxmlAttribute("button-type")]
        public Button.Modifiers.ButtonType Type
        {
            get => modifiers.Type;
            set => modifiers.Type = value;
        }

        [UxmlAttribute("button-style")]
        public Button.Modifiers.ButtonStyle ShowIcon {
            get => modifiers.ShowIcon;
            set => modifiers.ShowIcon = value;
        }

        [UxmlAttribute("button-icon-position")]
        public Button.Modifiers.ButtonIconPosition IconPosition
        {
            get => modifiers.IconPosition;
            set => modifiers.IconPosition = value;
        }

        [UxmlAttribute("icon")]
        public IconImage Image
        {
            get => Icon.Image;
            set => Icon.Image = value;
        }

        [UxmlAttribute("LabelText")]
        public string LabelText
        {
            get => Label.text;
            set => Label.text = value;
        }

        public ToggleButton()
        {
            modifiers = new(this);

            this.CloneComponentTree("Components");
        }
    }
}