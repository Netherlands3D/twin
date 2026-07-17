using Netherlands3D.Services;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class FPVExitBar : VisualElement
    {
        private Slider slider;
        // private VisualElement progressBar;

        public FPVExitBar()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            slider = this.Q<Slider>();
            // progressBar = this.Q<VisualElement>("ProgressBar");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            schedule.Execute(()=>
                ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>().Input.ExitDuration.AddListener(UpdateTimer)
            );
        }

        private void UpdateTimer(float percentage)
        {
            if (percentage == -1)
            {
                slider.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            }
            else
            {
                slider.EnableInClassList(UtilityClassConstants.HIDDEN, false);
                slider.value = percentage;
                // progressBar.style.width = Length.Percent(percentage * 100f);
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>().Input.ExitDuration.RemoveListener(UpdateTimer);
        }
    }
}