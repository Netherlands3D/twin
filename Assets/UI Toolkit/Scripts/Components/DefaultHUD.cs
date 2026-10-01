using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DefaultHUD : VisualElement
    {
        public DefaultHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            SetupPresentationModeToggles();
        }

        private void SetupPresentationModeToggles()
        {
            var buttons = this.Query<UnityEngine.UIElements.Toggle>(className:"default-hud__presentation-mode-toggle").ToList();

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