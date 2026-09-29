using System.Collections.Generic;
using Netherlands3D.Twin.PresentationModus;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin
{
    public class PresentationModeService : MonoBehaviour
    {
        [SerializeField] private PresentationModeFunctionality presentationModeFunctionality;
        
        private const string HiddenClass = "presentation-section--hidden";
        private const string ActiveRevealZoneClass = "presentation-reveal-zone--active";

        public class HideableSection
        {
            public VisualElement SectionElement { get; }
            public VisualElement RevealZoneElement { get; }
            public PinToggle PinToggleElement { get; }

            public HideableSection(VisualElement sectionElement, VisualElement revealZoneElement, PinToggle pinToggleElement)
            {
                SectionElement = sectionElement;
                RevealZoneElement = revealZoneElement;
                PinToggleElement = pinToggleElement;
            }
        }
        
        private HashSet<HideableSection> hideableSections = new();

        private readonly Dictionary<HideableSection, EventCallback<PointerEnterEvent>> revealZoneEnterCallbacks = new();
        private readonly Dictionary<HideableSection, EventCallback<PointerLeaveEvent>> revealZoneLeaveCallbacks = new();
        private readonly Dictionary<HideableSection, EventCallback<PointerEnterEvent>> sectionEnterCallbacks = new();
        private readonly Dictionary<HideableSection, EventCallback<PointerLeaveEvent>> sectionLeaveCallbacks = new();
        private readonly Dictionary<HideableSection, EventCallback<ChangeEvent<bool>>> pinChangedCallbacks = new();

        public void RegisterHideableSection(HideableSection hideableSection)
        {
            if (!hideableSections.Add(hideableSection))
            {
                return;
            }

            var section = hideableSection.SectionElement;
            var revealZone = hideableSection.RevealZoneElement;
            var pin = hideableSection.PinToggleElement;

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
                section.EnableInClassList("presentation-section--unpinned", !pinned);
            }

            EventCallback<PointerEnterEvent> revealZoneEnterCallback = _ => Show();
            revealZoneEnterCallbacks.Add(hideableSection, revealZoneEnterCallback);
            revealZone.RegisterCallback<PointerEnterEvent>(revealZoneEnterCallback);
            
            EventCallback<PointerLeaveEvent> revealZoneLeaveCallback = _ => Hide();
            revealZoneLeaveCallbacks.Add(hideableSection, revealZoneLeaveCallback);
            revealZone.RegisterCallback<PointerLeaveEvent>(revealZoneLeaveCallback);

            EventCallback<PointerEnterEvent> sessionEnterCallback = _ => Show();
            sectionEnterCallbacks.Add(hideableSection, sessionEnterCallback);
            section.RegisterCallback<PointerEnterEvent>(sessionEnterCallback);
            
            EventCallback<PointerLeaveEvent> sessionLeaveCallback = _ => Hide();
            sectionLeaveCallbacks.Add(hideableSection, sessionLeaveCallback);
            section.RegisterCallback<PointerLeaveEvent>(sessionLeaveCallback);

            EventCallback <ChangeEvent<bool>> pinChangedCallback = evt => SetPinned(evt.newValue);
            pinChangedCallbacks.Add(hideableSection, pinChangedCallback);
            pin.RegisterValueChangedCallback<bool>(pinChangedCallback);

            SetPinned(pin.value);
        }

        public void UnregisterHideableSection(HideableSection hideableSection)
        {
            if (!hideableSections.Remove(hideableSection))
            {
                return;
            }
            
            var section = hideableSection.SectionElement;
            var revealZone = hideableSection.RevealZoneElement;
            var pin = hideableSection.PinToggleElement;
            
            revealZone.UnregisterCallback<PointerEnterEvent>(revealZoneEnterCallbacks[hideableSection]);
            revealZoneEnterCallbacks.Remove(hideableSection);
            
            revealZone.UnregisterCallback<PointerLeaveEvent>(revealZoneLeaveCallbacks[hideableSection]);
            revealZoneLeaveCallbacks.Remove(hideableSection);
            
            section.UnregisterCallback<PointerEnterEvent>(sectionEnterCallbacks[hideableSection]);
            sectionEnterCallbacks.Remove(hideableSection);
            
            section.UnregisterCallback<PointerLeaveEvent>(sectionLeaveCallbacks[hideableSection]);
            sectionLeaveCallbacks.Remove(hideableSection);
            
            pin.UnregisterValueChangedCallback<bool>(pinChangedCallbacks[hideableSection]);
            pinChangedCallbacks.Remove(hideableSection);
            
        }

    }
}
