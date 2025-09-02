using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Toggle : UnityEngine.UIElements.Toggle
    {
        private Icon Icon => this.Q<Icon>("Icon");
        private Label Label => this.Q<Label>("Label");

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

        private ToggleStyle toggleStyle = ToggleStyle.WithIcon;
        [UxmlAttribute("toggle-style")]
        public ToggleStyle ShowIcon
        {
            get => toggleStyle;
            set => this.SetFieldValueAndReplaceClassName(ref toggleStyle, value, "toggle-style-");
        }

        private ToggleIconPosition toggleIconPosition = ToggleIconPosition.Left;
        [UxmlAttribute("toggle-icon-position")]
        public ToggleIconPosition IconPosition
        {
            get => toggleIconPosition;
            set => this.SetFieldValueAndReplaceClassName(ref toggleIconPosition, value, "toggle-icon-position-");
        }

        [UxmlAttribute("icon")]
        public Icon.IconImage Image
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
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/" + nameof(Toggle));
            asset.CloneTree(this);

            // Find and load USS stylesheet specific for this component
            var styleSheet = Resources.Load<StyleSheet>("UI/" + nameof(Toggle) + "-style");
            styleSheets.Add(styleSheet);

            AddToClassList("toggle");
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ApplyCurrentVariantClasses();
            });
        }

        private void ApplyCurrentVariantClasses()
        {
            EnableInClassList("toggle-style-normal", toggleStyle == ToggleStyle.Normal);
            EnableInClassList("toggle-style-with-icon", toggleStyle == ToggleStyle.WithIcon);
            EnableInClassList("toggle-style-icon-only", toggleStyle == ToggleStyle.IconOnly);

            EnableInClassList("toggle-icon-position-left", toggleIconPosition == ToggleIconPosition.Left);
            EnableInClassList("toggle-icon-position-right", toggleIconPosition == ToggleIconPosition.Right);
        }

    }
}