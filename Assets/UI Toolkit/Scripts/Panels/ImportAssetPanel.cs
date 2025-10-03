using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class ImportAssetPanel : BaseInspectorContentPanel
    {
        public ImportAssetPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
        }

        public override string GetTitle() => "Importeren";
        
        public void Show()
        {
            EnableInClassList("active", true);
        }

        public void Hide()
        {
            EnableInClassList("active", false);
        }
    }
}
