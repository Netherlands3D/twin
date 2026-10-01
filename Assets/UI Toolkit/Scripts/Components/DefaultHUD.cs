using Netherlands3D.Twin;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DefaultHUD : VisualElement
    {
        private PresentationModeService.HideableSection leftHideableSection;
        private PresentationModeService.HideableSection topHideableSection;
        private PresentationModeService.HideableSection bottomHideableSection;
        
        public DefaultHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            SetupPresentationButtons();
        }

        private void SetupPresentationButtons()
        {
            var buttons = this.Query<UnityEngine.UIElements.Toggle>("Presentation").ToList();

            foreach (var button in buttons)
            {
                button.RegisterValueChangedCallback(evt =>
                {
                    EnableInClassList("default-hud--presenting", evt.newValue);

                    foreach (var otherButton in buttons)
                    {
                        otherButton.SetValueWithoutNotify(evt.newValue);
                    }
                });
            }
        }
    }
}