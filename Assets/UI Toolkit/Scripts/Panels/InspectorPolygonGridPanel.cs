using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class InspectorPolygonGridPanel : BaseInspectorContentPanel
    {
        public override string Title => "Tekengebied grid selecteren";
        
        public InspectorPolygonGridPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += () => Show(true);
            OnHide += () => Show(false);
         
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
    }
}