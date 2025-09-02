using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Button : UnityEngine.UIElements.Button
    {
        private Icon Icon => this.Q<Icon>("Icon");
        private Label Label => this.Q<Label>("Label");
        
        public enum ButtonStyle
        {
            Normal,
            WithIcon,
            IconOnly
        }

        public enum ButtonIconPosition
        {
            Left,
            Right
        }

        private ButtonStyle buttonStyle = ButtonStyle.WithIcon;
        [UxmlAttribute("button-style")]
        public ButtonStyle ShowIcon
        {
            get => buttonStyle;
            set => this.SetFieldValueAndReplaceClassName(ref buttonStyle, value, "button-style-");
        }
        
        private ButtonIconPosition buttonIconPosition = ButtonIconPosition.Left;
        [UxmlAttribute("button-icon-position")]
        public ButtonIconPosition IconPosition  
        {
            get => buttonIconPosition;
            set => this.SetFieldValueAndReplaceClassName(ref buttonIconPosition, value, "button-icon-position-");
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

        public Button()
        {
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/" + nameof(Button));
            asset.CloneTree(this);

            // Find and load USS stylesheet specific for this component
            var styleSheet = Resources.Load<StyleSheet>("UI/" + nameof(Button) + "-style");
            styleSheets.Add(styleSheet);

            AddToClassList("button");
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ApplyCurrentVariantClasses();
            });
        }

        private void ApplyCurrentVariantClasses()
        {
            // Style variant
            EnableInClassList("button-style-normal", buttonStyle == ButtonStyle.Normal);
            EnableInClassList("button-style-with-icon", buttonStyle == ButtonStyle.WithIcon);
            EnableInClassList("button-style-icon-only", buttonStyle == ButtonStyle.IconOnly);

            // Icon position
            EnableInClassList("button-icon-position-left", buttonIconPosition == ButtonIconPosition.Left);
            EnableInClassList("button-icon-position-right", buttonIconPosition == ButtonIconPosition.Right);
        }
    }
}