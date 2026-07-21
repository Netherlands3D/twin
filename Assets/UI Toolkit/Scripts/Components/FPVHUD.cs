using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class FPVHUD : VisualElement
    {
        public FPVHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}