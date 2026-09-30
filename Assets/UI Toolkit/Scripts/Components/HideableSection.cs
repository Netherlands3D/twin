using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{

    [UxmlElement]
    public partial class HideableSection : VisualElement
    {

        public HideableSection()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
        
        [UxmlAttribute("reveal-zone")]
        public string RevealZoneName { get; set; }
        
        [UxmlAttribute("pin-toggle")]
        public string PinToggleName { get; set; }
    }
}