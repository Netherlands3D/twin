using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// NL3D TextField with optional icon and label variants.
    /// Loads UXML/USS from Resources/UI and exposes UXML attributes.
    /// </summary>
    [UxmlElement]
    public partial class TextField : UnityEngine.UIElements.TextField
    {
        public enum TextFieldStyle
        {
            Normal,
            WithLabel
        }

        public enum TextStyle
        {
            Base,
            Header,
            InspectorHeader
        }

        private TextFieldStyle fieldStyle = TextFieldStyle.Normal;
        [UxmlAttribute("textfield-style")]
        public TextFieldStyle Style
        {
            get => fieldStyle;
            set
            {
                this.SetFieldValueAndReplaceClassName(ref fieldStyle, value, "textfield-style-");
                ApplyStyleVariant();
            }
        }

        /// <summary>
        /// Ensures reasonable defaults per style. Keeps label text even when label is visually hidden,
        /// so it can serve as an accessible name or for debugging/QA.
        /// </summary>
        private void ApplyStyleVariant()
        {
            if (fieldStyle == TextFieldStyle.WithLabel)
            {
                // Provide a default if none was set
                if (string.IsNullOrEmpty(LabelText))
                    LabelText = "Label";
            }
        }

        private string labelText = string.Empty;
        [UxmlAttribute("label")]
        public string LabelText
        {
            get => label;   // BaseField<string>.label
            set
            {
                labelText = value;
                label = value ?? string.Empty;
            }
        }

        private TextStyle textStyle = TextStyle.Base;
        /// <summary>Applies theme text classes (text-base, text-header, text-inspector-header).</summary>
        [UxmlAttribute("text-style")]
        public TextStyle Typography
        {
            get => textStyle;
            set => this.SetFieldValueAndReplaceClassName(ref textStyle, value, "text-");
        }

        private bool password;
        /// <summary>Masks input characters when true.</summary>
        [UxmlAttribute("password")]
        public bool Password
        {
            get => password;
            set
            {
                password = value;
                isPasswordField = password;
            }
        }

        public TextField()
        {
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/" + nameof(TextField));
            asset.CloneTree(this);

            // Find and load USS stylesheet specific for this component
            var styleSheet = Resources.Load<StyleSheet>("UI/" + nameof(TextField) + "-style");
            styleSheets.Add(styleSheet);

            ApplyStyleVariant();
        }
    }
}