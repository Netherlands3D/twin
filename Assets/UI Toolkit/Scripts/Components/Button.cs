using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Button : UnityEngine.UIElements.Button
    {
        /// <summary>
        /// Define a shared set of modifiers between various button instances. Most buttons inherit their behaviour
        /// from this class, but some -like the ToggleButton- inherit their basic behaviour from a different class
        /// and can use this as a shared set of button behaviours.
        /// </summary>
        public class Modifiers
        {
            public enum ButtonType
            {
                Standard,
                Transparent
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

            private readonly VisualElement button;

            private ButtonType buttonType = ButtonType.Standard;
            public ButtonType Type
            {
                get => buttonType;
                set { buttonType = value; UpdateClassList(); }
            }

            private ButtonStyle buttonStyle = ButtonStyle.WithIcon;
            public ButtonStyle ShowIcon
            {
                get => buttonStyle;
                set { buttonStyle = value; UpdateClassList(); }
            }

            private ButtonIconPosition buttonIconPosition = ButtonIconPosition.Left;

            public ButtonIconPosition IconPosition
            {
                get => buttonIconPosition;
                set { buttonIconPosition = value; UpdateClassList(); }
            }
            
            public Modifiers(VisualElement button)
            {
                this.button = button;
                button.RegisterCallback<AttachToPanelEvent>(OnAttachPanelEvent);
            }
            
            ~Modifiers() {
                button.UnregisterCallback<AttachToPanelEvent>(OnAttachPanelEvent);
            }

            private void OnAttachPanelEvent(AttachToPanelEvent _)
            {
                UpdateClassList();
            }

            private void UpdateClassList()
            {
                button.ReplacePrefixedValueInClassList("button-type-", buttonType.ToString().ToKebabCase());
                button.ReplacePrefixedValueInClassList("button-style-", buttonStyle.ToString().ToKebabCase());
                button.ReplacePrefixedValueInClassList("button-icon-position-", buttonIconPosition.ToString().ToKebabCase());
            }
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

        private readonly Modifiers modifiers;
        [UxmlAttribute("button-type")]
        public Modifiers.ButtonType Type
        {
            get => modifiers.Type;
            set => modifiers.Type = value;
        }

        [UxmlAttribute("button-style")]
        public Modifiers.ButtonStyle ShowIcon {
            get => modifiers.ShowIcon;
            set => modifiers.ShowIcon = value;
        }

        [UxmlAttribute("button-icon-position")]
        public Modifiers.ButtonIconPosition IconPosition
        {
            get => modifiers.IconPosition;
            set => modifiers.IconPosition = value;
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
            modifiers = new(this);
            this.CloneComponentTree("Components");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
        }

        private void OnAttachToPanelEvent(AttachToPanelEvent _)
        {
            // If a type label was provided via UXML attribute, ensure it is reflected on the element
            if (!string.IsNullOrEmpty(typeLabel) && TypeLabelElement != null) TypeLabelElement.text = typeLabel;

            ApplyTypeBadge();
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
