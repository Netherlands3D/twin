using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class CredentialPanel : VisualElement
    {
        public CredentialPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
        }
    }
}