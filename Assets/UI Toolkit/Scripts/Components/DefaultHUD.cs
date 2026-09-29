using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.PresentationModus.UIHider;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DefaultHUD : VisualElement
    {
        private const string HiddenClass = "presentation-section--hidden";
        private const string ActiveRevealZoneClass = "presentation-reveal-zone--active";

        private PresentationModeService PresentationModeService => ServiceLocator.GetService<PresentationModeService>();

        private PresentationModeService.HideableSection leftHideableSection;
        private PresentationModeService.HideableSection topHideableSection;
        private PresentationModeService.HideableSection bottomHideableSection;
        
        public DefaultHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            SetupPresentationButtons();
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            leftHideableSection ??= new PresentationModeService.HideableSection(
                this.Q<VisualElement>("LeftSection"),
                this.Q<VisualElement>("LeftRevealZone"),
                this.Q<PinToggle>("LeftPresentationPin"));
            PresentationModeService.RegisterHideableSection(leftHideableSection);
            
            topHideableSection ??= new PresentationModeService.HideableSection(
                this.Q<VisualElement>("TopSection"),
                this.Q<VisualElement>("TopRevealZone"),
                this.Q<PinToggle>("PresentationPin"));
            PresentationModeService.RegisterHideableSection(topHideableSection);
            
            bottomHideableSection ??= new PresentationModeService.HideableSection(
                this.Q<VisualElement>("BottomSection"),
                this.Q<VisualElement>("BottomRevealZone"),
                this.Q<PinToggle>("NavigationPresentationPin"));
            PresentationModeService.RegisterHideableSection(bottomHideableSection);
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            PresentationModeService.UnregisterHideableSection(leftHideableSection);
            PresentationModeService.UnregisterHideableSection(topHideableSection);
            PresentationModeService.UnregisterHideableSection(bottomHideableSection);
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