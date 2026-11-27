using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DashboardHeader : VisualElement
    {
        private readonly Label title;
        private readonly VisualElement logo;

        public DashboardHeader()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            title = this.Q<Label>(className: "dashboard-hud__title");
            logo = this.Q("DashboardLogo");
            logo.RegisterCallback<GeometryChangedEvent>(_ => UpdateLogoWidth());
        }

        internal void SetProjectTitle(string projectTitle)
        {
            title.text = string.IsNullOrWhiteSpace(projectTitle) ? "Dashboard" : $"Dashboard | {projectTitle}";
            title.tooltip = title.text;
        }

        internal void SetLogo(Texture2D texture)
        {
            logo.style.backgroundImage = texture != null ? new StyleBackground(texture) : new StyleBackground(StyleKeyword.None);
            logo.EnableInClassList(UtilityClassConstants.HIDDEN, texture == null);
            UpdateLogoWidth();
        }

        private void UpdateLogoWidth()
        {
            var texture = logo.style.backgroundImage.value.texture;
            if (texture == null)
            {
                logo.style.flexBasis = StyleKeyword.Auto;
                return;
            }

            var height = logo.resolvedStyle.height;
            if (!float.IsNaN(height) && height > 0)
                logo.style.flexBasis = height * texture.width / texture.height;
        }
    }
}
