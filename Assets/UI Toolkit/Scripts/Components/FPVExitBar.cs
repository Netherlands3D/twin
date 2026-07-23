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

        public FPVExitBar()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            slider = this.Q<Slider>();
            slider.EnableInClassList(UtilityClassConstants.HIDDEN, true);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            schedule.Execute(() => //wait a frame for the service to be available
            {
                slider.Q<NumberField>().EnableInClassList(UtilityClassConstants.HIDDEN, true);
                ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>()?.Input.ExitDuration.AddListener(UpdateTimer);
            });
        }

        private void UpdateTimer(float percentage)
        {
            if (percentage < 0)
            {
                slider.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            }
            else
            {
                slider.EnableInClassList(UtilityClassConstants.HIDDEN, false);
                slider.value = percentage;
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>()?.Input.ExitDuration.RemoveListener(UpdateTimer);
        }
    }
}