using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// Collapsible content container based on UI Toolkit Foldout.
    /// - Leading icon in UXML, image via CSS by default; only set Icon.Image when 'leading-icon' is provided.
    /// - Help icon in UXML, image via CSS (--icon-image) on .help-icon .icon.
    /// Mirrors the Button/Icon pattern.
    /// </summary>
    [UxmlElement]
    public partial class ContentContainer : Foldout
    {
        // Foldout internals
        private VisualElement HeaderInput => this.Q<VisualElement>(className: "unity-toggle__input");
        private Label HeaderLabel => this.Q<Label>(className: "unity-label");
        private VisualElement Checkmark => this.Q<VisualElement>(className: "unity-foldout__checkmark");

        // Elements from UXML
        private Icon leadingIcon => this.Q<Icon>("LeadingIcon");
        private HelpButton helpButton => this.Q<HelpButton>("HelpButton");

        public enum ContainerType
        {
            Foldout,
            NoFoldout
        }

        private ContainerType containerType = ContainerType.Foldout;
        private const string HideCheckmarkClass = "hide-checkmark";

        public enum ContainerStyle
        {
            Normal,
            WithIcon
        }

        private ContainerStyle containerStyle = ContainerStyle.WithIcon;

        [UxmlAttribute("container-type")]
        public ContainerType Type
        {
            get => containerType;
            set
            {
                this.SetFieldValueAndReplaceClassName(ref containerType, value, "container-type-");
                ApplyContainerType();
            }
        }

        [UxmlAttribute("container-style")]
        public ContainerStyle ShowIcon
        {
            get => containerStyle;
            set
            {
                this.SetFieldValueAndReplaceClassName(ref containerStyle, value, "container-style-");
                UpdateIcons();
                ReorderHeaderChildren();
            }
        }

        [UxmlAttribute("text")]
        public string HeaderText
        {
            get => text;
            set => text = value;
        }

        [UxmlAttribute("expanded")]
        public bool Expanded
        {
            get => value;
            set { this.value = value; }
        }

        private bool showDivider = true;

        /// <summary>Show or hide the divider (independent of expanded state).</summary>
        [UxmlAttribute("show-divider")]
        public bool ShowDivider
        {
            get => showDivider;
            set
            {
                showDivider = value;
                SetDividerVisibility();
            }
        }

        [UxmlAttribute("leading-icon")]
        public IconImage LeadingIconImage
        {
            get => leadingIcon.Image;
            set => leadingIcon.Image = value;
        }

        private bool showHelpIcon;

        [UxmlAttribute("show-help-icon")]
        public bool ShowHelpIcon
        {
            get => showHelpIcon;
            set
            {
                showHelpIcon = value;
                UpdateIcons();
                ReorderHeaderChildren();
            }
        }

        private string helpUrl;
        private VisualElement headerDivider;

        [UxmlAttribute("help-url")]
        public string HelpUrl
        {
            get => helpUrl;
            set
            {
                helpUrl = value;
                if (helpButton != null)
                    helpButton.HelpUrl = value;
            }
        }

        public ContentContainer()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            if (string.IsNullOrEmpty(text)) text = "Label";

            // Block collapse when NoFoldout is active (mouse/keyboard/script)
            RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if (containerType == ContainerType.NoFoldout && !evt.newValue)
                    value = true;
            });

            // Setup after attach (Foldout internals exist)
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ApplyContainerType();
                ReorderHeaderChildren();
                EnsureDividerPosition();
                UpdateIcons();
                SetDividerVisibility();

                if (!string.IsNullOrEmpty(helpUrl) && helpButton != null)
                    helpButton.HelpUrl = helpUrl;
            });
        }

        /// <summary>
        /// Visual order: LeadingIcon, Label, HelpIcon, Checkmark. DOM == visual.
        /// </summary>
        private void ReorderHeaderChildren()
        {
            var input = HeaderInput;
            if (input == null) return;
            var label = HeaderLabel;
            var check = Checkmark;
            if (check == null) return;

            if (leadingIcon != null && leadingIcon.parent != input) input.Add(leadingIcon);
            if (label != null && label.parent != input) input.Add(label);
            if (helpButton != null && helpButton.parent != input) input.Add(helpButton);
            if (check.parent != input) input.Add(check);

            int i = 0;
            if (leadingIcon != null) input.Insert(i++, leadingIcon);
            if (label != null) input.Insert(i++, label);
            if (helpButton != null) input.Insert(i++, helpButton);
            input.Insert(i, check);
        }

        /// <summary>
        /// Force Nofoldout style on ContentContainer
        /// </summary>
        private void ApplyContainerType()
        {
            // force expanded when NoFoldout
            if (containerType == ContainerType.NoFoldout && !this.value)
                this.value = true;

            var input = HeaderInput;
            if (input != null)
                input.pickingMode = (containerType == ContainerType.NoFoldout)
                    ? PickingMode.Ignore
                    : PickingMode.Position;

            // Mouse-interaction off when NoFoldout
            var check = Checkmark;
            if (check != null)
                check.EnableInClassList(HideCheckmarkClass, containerType == ContainerType.NoFoldout);
        }

        /// <summary>
        /// Place the Divider as the first child inside the content container,
        /// so user content always appears below it.
        /// </summary>
        private void EnsureDividerPosition()
        {
            if (headerDivider == null)
            {
                headerDivider = new VisualElement { name = "Divider" };
                headerDivider.AddToClassList("divider");
                headerDivider.AddToClassList("divider-header");
            }
            if (headerDivider.parent != contentContainer)
            {
                contentContainer.Insert(0, headerDivider);
            }
        }

        /// <summary>
        /// Divider visibility is controlled only by showDivider.
        /// Foldout collapse/expand already hides/shows the entire content container.
        /// </summary>
        private void SetDividerVisibility()
        {
            EnableInClassList("divider-active", showDivider);
        }

        private void UpdateIcons()
        {
            bool showLeading = (containerStyle == ContainerStyle.WithIcon);
            if (leadingIcon != null)
                leadingIcon.style.display = showLeading ? DisplayStyle.Flex : DisplayStyle.None;

            if (helpButton != null)
                helpButton.style.display = showHelpIcon ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}