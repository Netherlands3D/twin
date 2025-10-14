using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Toggle : UnityEngine.UIElements.Toggle, IComponent
    {
        public enum ToggleStyle
        {
            Normal,
            WithIcon,
            IconOnly
        }

        public enum ToggleIconPosition
        {
            Left,
            Right
        }

        // Query and cache icon component
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>("Icon");

        // Query and cache label component
        private Label labelField;
        private Label Label => labelField ??= this.Q<Label>("Label");

        private ToggleStyle toggleStyle = ToggleStyle.WithIcon;
        [UxmlAttribute("toggle-style")]
        public ToggleStyle ShowIcon
        {
            get => toggleStyle;
            set { toggleStyle = value; UpdateClassList(); }
        }

        private ToggleIconPosition toggleIconPosition = ToggleIconPosition.Left;
        [UxmlAttribute("toggle-icon-position")]
        public ToggleIconPosition IconPosition
        {
            get => toggleIconPosition;
            set { toggleIconPosition = value; UpdateClassList(); }
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

        public Toggle()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<AttachToPanelEvent>(_ => UpdateClassList());
        }

        private void UpdateClassList()
        {
            this.ReplacePrefixedValueInClassList("toggle-style-", toggleStyle.ToString().ToKebabCase());
            this.ReplacePrefixedValueInClassList("toggle-icon-position-", toggleIconPosition.ToString().ToKebabCase());
        }
    }
}