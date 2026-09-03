using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class HelpButton : ChangePointerStyleElement
    {
        private Icon Icon => this.Q<Icon>("Icon");

        [UxmlAttribute("icon")]
        public string Image
        {
            get => Icon.Image;
            set => Icon.Image = value;
        }

        private string helpUrl;

        [UxmlAttribute("help-url")]
        public string HelpUrl
        {
            get => helpUrl;
            set
            {
                helpUrl = value;
                StyleOnHover = string.IsNullOrEmpty(helpUrl) ? PointerStyle.Auto : PointerStyle.Pointer;
            }
        }

        public HelpButton()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            RegisterCallback<ClickEvent>(OnClick);
            
            StyleOnHover = PointerStyle.Auto;
        }
        
        private void OnClick(ClickEvent evt)
        {
            if (!string.IsNullOrEmpty(helpUrl))
                Application.OpenURL(helpUrl);
        }
    }
}