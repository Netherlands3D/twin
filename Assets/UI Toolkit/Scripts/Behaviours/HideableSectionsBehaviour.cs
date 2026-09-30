using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    public class HideableSectionsBehaviour : MonoBehaviour
    {
        
        private const string HiddenClass = "presentation-section--hidden";
        private const string ActiveRevealZoneClass = "presentation-reveal-zone--active";
        
        private HashSet<HideableSection> hideableSections = new();

        private readonly Dictionary<HideableSection, EventCallback<PointerEnterEvent>> revealZoneEnterCallbacks = new();
        private readonly Dictionary<HideableSection, EventCallback<PointerLeaveEvent>> revealZoneLeaveCallbacks = new();
        private readonly Dictionary<HideableSection, EventCallback<PointerEnterEvent>> sectionEnterCallbacks = new();
        private readonly Dictionary<HideableSection, EventCallback<PointerLeaveEvent>> sectionLeaveCallbacks = new();
        private readonly Dictionary<HideableSection, EventCallback<ChangeEvent<bool>>> pinChangedCallbacks = new();

        private void Awake()
        {
            var root = App.UIRoot.Root;

            root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RegisterHideableSections();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterHideableSections();
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterHideableSections();
        }

        private void RegisterHideableSections()
        {
            var root = App.UIRoot.Root;

            var sections = root.Query<HideableSection>().ToList();

            foreach (var hideableSection in sections)
            {
                RegisterHideableSection(hideableSection);
            }
        }

        private void RegisterHideableSection(HideableSection hideableSection)
        {
            if (!hideableSections.Add(hideableSection))
            {
                return;
            }
            
            GetHideableElements(hideableSection, out var revealZone, out var pinToggle);

            EventCallback<PointerEnterEvent> sessionEnterCallback = _ => Show();
            sectionEnterCallbacks.Add(hideableSection, sessionEnterCallback);
            hideableSection.RegisterCallback<PointerEnterEvent>(sessionEnterCallback);
            
            EventCallback<PointerLeaveEvent> sessionLeaveCallback = _ => Hide();
            sectionLeaveCallbacks.Add(hideableSection, sessionLeaveCallback);
            hideableSection.RegisterCallback<PointerLeaveEvent>(sessionLeaveCallback);

            if (revealZone != null)
            {
                EventCallback<PointerEnterEvent> revealZoneEnterCallback = _ => Show();
                revealZoneEnterCallbacks.Add(hideableSection, revealZoneEnterCallback);
                revealZone.RegisterCallback<PointerEnterEvent>(revealZoneEnterCallback);

                EventCallback<PointerLeaveEvent> revealZoneLeaveCallback = _ => Hide();
                revealZoneLeaveCallbacks.Add(hideableSection, revealZoneLeaveCallback);
                revealZone.RegisterCallback<PointerLeaveEvent>(revealZoneLeaveCallback);
            }

            if (pinToggle != null)
            {
                EventCallback<ChangeEvent<bool>> pinChangedCallback = evt => SetPinned(evt.newValue);
                pinChangedCallbacks.Add(hideableSection, pinChangedCallback);
                pinToggle.RegisterValueChangedCallback<bool>(pinChangedCallback);
            }

            if (pinToggle != null)
            {
                SetPinned(pinToggle.value);
            }

            return;

            void Show()
            {
                if (pinToggle != null && !pinToggle.value)
                {
                    hideableSection.RemoveFromClassList(HiddenClass);
                }
            }
            
            
            void Hide()
            {
                if (pinToggle != null && !pinToggle.value)
                {
                    hideableSection.AddToClassList(HiddenClass);
                }
            }

            void SetPinned(bool pinned)
            {
                //When pinned, disable the revealzone, and show section.
                //When unpinnned, enabled the revealzone, and hide section.
                revealZone?.EnableInClassList(ActiveRevealZoneClass, !pinned);
                hideableSection.EnableInClassList(HiddenClass, !pinned);
                hideableSection.EnableInClassList("presentation-section--unpinned", !pinned);
            }
        }
        
        private void UnregisterHideableSections()
        {
            foreach (var hideableSection in hideableSections.ToArray())
            {
                UnregisterHideableSection(hideableSection);
            }
        }
        
        private void UnregisterHideableSection(HideableSection hideableSection)
        {
            hideableSections.Remove(hideableSection);
            
            GetHideableElements(hideableSection, out var revealZone, out var pinToggle);

            if (revealZoneEnterCallbacks.Remove(hideableSection, out var revealZoneEnterCallback))
                revealZone?.UnregisterCallback<PointerEnterEvent>(revealZoneEnterCallback);

            if (revealZoneLeaveCallbacks.Remove(hideableSection, out var revealZoneLeaveCallback))
                revealZone?.UnregisterCallback<PointerLeaveEvent>(revealZoneLeaveCallback);

            if (sectionEnterCallbacks.Remove(hideableSection, out var sectionEnterCallback))
                hideableSection.UnregisterCallback<PointerEnterEvent>(sectionEnterCallback);

            if (sectionLeaveCallbacks.Remove(hideableSection, out var sectionLeaveCallback))
                hideableSection.UnregisterCallback<PointerLeaveEvent>(sectionLeaveCallback);

            if (pinChangedCallbacks.Remove(hideableSection, out var pinChangedCallback))
                pinToggle?.UnregisterValueChangedCallback(pinChangedCallback);

            hideableSections.Remove(hideableSection);
        }

        private void GetHideableElements(HideableSection hideableSection, out VisualElement revealZone, out PinToggle pinToggle)
        {
            var root = App.UIRoot.Root;
            revealZone = hideableSection.RevealZoneName != null ? root.Q<VisualElement>(hideableSection.RevealZoneName) : null;
            pinToggle = hideableSection.PinToggleName != null ? root.Q<PinToggle>(hideableSection.PinToggleName) : null;
        }

    }
}
