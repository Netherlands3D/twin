using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// NL3D TextField with optional icon and label variants.
    /// Loads UXML/USS from Resources/UI and exposes UXML attributes.
    /// </summary>
    [UxmlElement]
    public partial class TextField : UnityEngine.UIElements.TextField, IComponent
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
                fieldStyle = value;
                ApplyStyleVariant();
                UpdateClassList();
            }
        }

        private string labelText = string.Empty;
        [UxmlAttribute("label")]
        public string LabelText
        {
            get => label;
            set { labelText = value; label = value ?? string.Empty; }
        }

        private TextStyle textStyle = TextStyle.Base;
        [UxmlAttribute("text-style")]
        public TextStyle Typography
        {
            get => textStyle;
            set { textStyle = value; UpdateClassList(); }
        }

        /// <summary>Masks input characters when true.</summary>
        private bool password = false;
        [UxmlAttribute("password")]
        public bool Password
        {
            get => password;
            set { password = value; isPasswordField = password; }
        }

        public TextField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ApplyStyleVariant();
                UpdateClassList();
            });
        }
        
        /// <summary>
        /// Ensures reasonable defaults per style. Keeps label text even when label is visually hidden,
        /// so it can serve as an accessible name or for debugging/QA.
        /// </summary>
        private void ApplyStyleVariant()
        {
            if (fieldStyle == TextFieldStyle.WithLabel)
            {
                if (string.IsNullOrEmpty(LabelText)) LabelText = "Label";
            }
        }
        
        private void UpdateClassList()
        {
            this.RemoveFromClassListStartingWith("text-");
            AddToClassList("text-" + textStyle.ToString().ToKebabCase());
            this.RemoveFromClassListStartingWith("textfield-style-");
            AddToClassList("textfield-style-" + fieldStyle.ToString().ToKebabCase());
        }
    }
}