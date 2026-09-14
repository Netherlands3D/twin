using System;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class KeyValue : VisualElement
    {
        // Query and cache label component
        private Label keyField;
        private Label KeyField => keyField ??= this.Q<Label>("Key");
        
        private Label valueField;
        private Label ValueField => valueField ??= this.Q<Label>("Value");

        private Hyperlink valueLink;
        private Hyperlink ValueLink => valueLink ??= this.Q<Hyperlink>("ValueLink");

        private VisualElement divider;

        [UxmlAttribute("key")]
        public string Key
        {
            get => KeyField.text;
            set => KeyField.text = value;
        }
        
        [UxmlAttribute("value")]
        public string Value
        {
            get => ValueField.text;
            set
            {
                var isUrl = IsWebUrl(value);
                ValueField.text = value;
                ValueField.EnableInClassList(UtilityClassConstants.HIDDEN, isUrl);
                
                ValueLink.text = value;
                ValueLink.url = value;
                ValueLink.EnableInClassList(UtilityClassConstants.HIDDEN, !isUrl);
            }
        }

        public KeyValue()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            divider = this.Q<VisualElement>("Divider");
        }

        public void ShowDivider(bool show)
        {
            divider.EnableInClassList(UtilityClassConstants.HIDDEN, !show);   
        }
        
        private static bool IsWebUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp
                       || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}