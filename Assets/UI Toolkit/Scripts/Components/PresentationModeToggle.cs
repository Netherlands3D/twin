using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class PresentationModeToggle : Toggle
    {
        public PresentationModeToggle()
        {
            this.AddComponentStylesheet("Components");
            
            Type = Toggle.ToggleType.transparent;
            ShowIcon = Toggle.ToggleStyle.IconOnly;
            Image = IconImage.PRESENTATION_CHART;
            tooltip = "Presentatiemodus (H)";
        }
    }
}