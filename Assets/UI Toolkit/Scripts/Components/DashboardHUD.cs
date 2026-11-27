using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DashboardHUD : VisualElement
    {
        public DashboardHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            var phaseView = this.Q<ToggleSwitch>("DashboardViewSwitch");
            var programOverview = this.Q("ProgramOverviewContent");
            var phaseProgress = this.Q("PhaseProgressContent");
            var phaseChanges = this.Q("PhaseChangesPanel");

            phaseView.RegisterValueChangedCallback(evt => ShowPhaseView(evt.newValue));
            ShowPhaseView(phaseView.value);

            void ShowPhaseView(bool showPhaseProgress)
            {
                programOverview.EnableInClassList(UtilityClassConstants.HIDDEN, showPhaseProgress);
                phaseProgress.EnableInClassList(UtilityClassConstants.HIDDEN, !showPhaseProgress);
                phaseChanges.EnableInClassList(UtilityClassConstants.HIDDEN, !showPhaseProgress);
            }
        }
    }
}
