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

            UpdateTimer(-1);
            
            schedule.Execute(()=>
                ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>()?.Input.ExitDuration.AddListener(UpdateTimer)
            );
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            ServiceLocator.GetService<FirstPersonViewer.FirstPersonViewer>()?.Input.ExitDuration.RemoveListener(UpdateTimer);
        }
    }
}