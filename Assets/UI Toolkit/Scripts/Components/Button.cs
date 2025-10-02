using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Button : UnityEngine.UIElements.Button
    {
        // Existing elements
        private Icon Icon => this.Q<Icon>("Icon");
        private Label Label => this.Q<Label>("Label");

        // New elements provided by UXML (robust class lookup)
        private VisualElement TypeDivider => this.Q<VisualElement>("Divider") ?? this.Q<VisualElement>(null, "divider");
        private Label TypeLabelElement => this.Q<Label>("TypeLabel") ?? this.Q<Label>(null, "type-label");

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

        // Variant / position (unchanged)
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
            set
            {
                typeLabel = value;   // kan null/empty zijn
                ApplyTypeBadge();    // tekst en zichtbaarheid centraal regelen
            }
        }


        // Pass-throughs
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

            // Find and load USS stylesheet specific for this component (using -style)
            var styleSheet = Resources.Load<StyleSheet>("UI/" + nameof(Button) + "-style");
            styleSheets.Add(styleSheet);

            // Base class + initial classes before layout
            AddToClassList("button");
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ApplyCurrentVariantClasses();

                // If a type label was provided via UXML attribute, ensure it is reflected on the element
                if (!string.IsNullOrEmpty(typeLabel) && TypeLabelElement != null)
                    TypeLabelElement.text = typeLabel;

                ApplyTypeBadge();
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
