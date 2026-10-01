using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class HideableSection : VisualElement
    {
        [UxmlAttribute("section-id")]
        public string SectionId { get; private set; }
        
        [UxmlAttribute("reveal-zone")]
        public string RevealZoneName { get; private set; }
        
        [UxmlAttribute("pin-toggle")]
        public string PinToggleName { get; private set; }
    }
}