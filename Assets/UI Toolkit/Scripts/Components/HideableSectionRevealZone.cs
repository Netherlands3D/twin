using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class HideableSectionRevealZone : VisualElement
    {
        public HideableSectionRevealZone()
        {
            this.AddComponentStylesheet("Components");
        }
    }
}