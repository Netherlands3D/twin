using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DebugInfo : VisualElement
    {
        public MemoryStats MemoryStats { get; private set; }
        public FPSIndicator FPSIndicator { get; private set; }
        public DebugInfo()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            MemoryStats = this.Q<MemoryStats>();
            FPSIndicator = this.Q<FPSIndicator>();
        }
    }
}