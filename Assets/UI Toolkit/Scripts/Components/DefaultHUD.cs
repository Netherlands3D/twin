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
        
        public DefaultHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            SetupAutoHideSection("LeftSection", "LeftRevealZone", "LeftPresentationPin");
            SetupAutoHideSection("TopSection", "TopRevealZone", "PresentationPin");
            SetupAutoHideSection("MovableBottomSection", "BottomRevealZone", "NavigationPresentationPin");
        }

        private void SetupAutoHideSection(string sectionName, string revealZoneName, string pinName)
        {
            var section = this.Q<VisualElement>(sectionName);
            var revealZone = this.Q<VisualElement>(revealZoneName);
            var pin = this.Q<PinToggle>(pinName);

            void Show()
            {
                if (!pin.value)
                    section.RemoveFromClassList(HiddenClass);
            }
            void Hide()
            {
                if (!pin.value)
                    section.AddToClassList(HiddenClass);
            }
            void SetPinned(bool pinned)
            {
                //When pinned, disable the revealzone, and show section.
                //When unpinnned, enabled the revealzone, and hide section.
                revealZone.EnableInClassList(ActiveRevealZoneClass, !pinned);
                section.EnableInClassList(HiddenClass, !pinned);
            }

            revealZone.RegisterCallback<PointerEnterEvent>(_ => Show());
            revealZone.RegisterCallback<PointerLeaveEvent>(_ => Hide());

            section.RegisterCallback<PointerEnterEvent>(_ => Show());
            section.RegisterCallback<PointerLeaveEvent>(_ => Hide());

            pin.RegisterValueChangedCallback(evt => SetPinned(evt.newValue));

            SetPinned(pin.value);
        }
        
        
    }
}