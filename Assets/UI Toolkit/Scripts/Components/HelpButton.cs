using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class HelpButton : UnityEngine.UIElements.Button, IComponent
    {
        private Icon Icon => this.Q<Icon>("Icon");

        [UxmlAttribute("icon")]
        public IconImage Image
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
                if (Icon != null) Icon.tooltip = string.IsNullOrEmpty(value) ? null : "Meer informatie";
            }
        }

        public HelpButton()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            if (string.IsNullOrEmpty(helpUrl))
                helpUrl = "Link naar documentatie";

            // Click navigates to HelpUrl when provided
            clicked += () =>
            {
                if (!string.IsNullOrEmpty(helpUrl))
                    Application.OpenURL(helpUrl);
            };
        }
    }
}