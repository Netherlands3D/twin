using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class HelpButton : UnityEngine.UIElements.Button
    {
        private Icon Icon => this.Q<Icon>("Icon");

        [UxmlAttribute("icon")]
        public Icon.IconImage Image
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
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/" + nameof(HelpButton));
            asset.CloneTree(this);

            // Find and load USS stylesheet specific for this component (using -style)
            var styleSheet = Resources.Load<StyleSheet>("UI/" + nameof(HelpButton) + "-style");
            styleSheets.Add(styleSheet);

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