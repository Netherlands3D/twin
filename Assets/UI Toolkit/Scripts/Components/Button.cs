using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Button : UnityEngine.UIElements.Button
    {
        public enum ButtonType
        {
            Standard,
            transparent
        }

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

        // Query and cache icon component
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>("Icon");

        // Query and cache label component
        private Label labelField;
        private Label Label => labelField ??= this.Q<Label>("Label");

        // New elements provided by UXML (robust class lookup)
        private VisualElement TypeDivider => this.Q<VisualElement>("Divider") ?? this.Q<VisualElement>(null, "divider");
        private Label TypeLabelElement => this.Q<Label>("TypeLabel") ?? this.Q<Label>(null, "type-label");

        private ButtonType buttonType = ButtonType.Standard;
        [UxmlAttribute("button-type")]
        public ButtonType Type
        {
            get => buttonType;
            set { buttonType = value; UpdateClassList(); }
        }

        private ButtonStyle buttonStyle = ButtonStyle.WithIcon;
        [UxmlAttribute("button-style")]
        public ButtonStyle ShowIcon
        {
            get => buttonStyle;
            set { buttonStyle = value; UpdateClassList(); }
        }

        private ButtonIconPosition buttonIconPosition = ButtonIconPosition.Left;
        [UxmlAttribute("button-icon-position")]
        public ButtonIconPosition IconPosition
        {
            get => buttonIconPosition;
            set { buttonIconPosition = value; UpdateClassList(); }
        }

        // Type badge config
        private bool showType;
        [UxmlAttribute("show-type")]
        public bool ShowType
        {
            get => showType;
            set { showType = value; ApplyTypeBadge(); }
        }

        private string typeLabel;
        [UxmlAttribute("type-label")]
        public string TypeLabel
        {
            get => typeLabel;
            set { typeLabel = value; ApplyTypeBadge(); }
        }


        // Pass-throughs
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

        public Button()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UpdateClassList();

                // If a type label was provided via UXML attribute, ensure it is reflected on the element
                if (!string.IsNullOrEmpty(typeLabel) && TypeLabelElement != null)
                    TypeLabelElement.text = typeLabel;

                ApplyTypeBadge();
            });
        }

        private void UpdateClassList()
        {
            this.ReplacePrefixedValueInClassList("button-type-", buttonType.ToString().ToKebabCase());
            this.ReplacePrefixedValueInClassList("button-style-", buttonStyle.ToString().ToKebabCase());
            this.ReplacePrefixedValueInClassList("button-icon-position-", buttonIconPosition.ToString().ToKebabCase());
        }

        /// <summary>
        /// Show/hide Divider and TypeLabel when show-type is enabled.
        /// Default text is "type" until an explicit type-label is provided.
        /// </summary>
        private void ApplyTypeBadge()
        {
            var typeEl = TypeLabelElement;
            if (typeEl == null) return;

            // Default: "type" totdat de Inspector een andere waarde zet
            string textToUse = !string.IsNullOrEmpty(typeLabel) ? typeLabel : "type";
            typeEl.text = textToUse;

            bool shouldShow = showType;
            EnableInClassList("show-type", shouldShow);
        }
    }
}
