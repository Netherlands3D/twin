using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    public class HideableSectionsBehaviour : MonoBehaviour
    {
        
        private const string HiddenClass = "hideable-section--hidden";
        private const string UnpinnedClass = "hideable-section--unpinned";
        private const string RevealZoneActiveClass = "hideable-section-reveal-zone--active";
        
        private readonly HashSet<HideableSection> hideableSections = new();

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

        private void OnDestroy()
        {
            var root = App.UIRoot.Root;
            
            root.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            root.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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
            
            ResolveLinkedElements(hideableSection, out var revealZone, out var pinToggle);

            EventCallback<PointerEnterEvent> sectionEnterCallback = _ => ShowSection();
            sectionEnterCallbacks.Add(hideableSection, sectionEnterCallback);
            hideableSection.RegisterCallback<PointerEnterEvent>(sectionEnterCallback);
            
            EventCallback<PointerLeaveEvent> sectionLeaveCallback = _ => HideSection();
            sectionLeaveCallbacks.Add(hideableSection, sectionLeaveCallback);
            hideableSection.RegisterCallback<PointerLeaveEvent>(sectionLeaveCallback);

            if (revealZone != null)
            {
                EventCallback<PointerEnterEvent> revealZoneEnterCallback = _ => ShowSection();
                revealZoneEnterCallbacks.Add(hideableSection, revealZoneEnterCallback);
                revealZone.RegisterCallback<PointerEnterEvent>(revealZoneEnterCallback);

                EventCallback<PointerLeaveEvent> revealZoneLeaveCallback = _ => HideSection();
                revealZoneLeaveCallbacks.Add(hideableSection, revealZoneLeaveCallback);
                revealZone.RegisterCallback<PointerLeaveEvent>(revealZoneLeaveCallback);
            }

            if (pinToggle != null)
            {
                EventCallback<ChangeEvent<bool>> pinChangedCallback = evt => ApplyPinnedState(evt.newValue);
                pinChangedCallbacks.Add(hideableSection, pinChangedCallback);
                pinToggle.RegisterValueChangedCallback<bool>(pinChangedCallback);
            }

            if (pinToggle != null)
            {
                ApplyPinnedState(pinToggle.value);
            }

            return;

            void ShowSection()
            {
                if (pinToggle != null && !pinToggle.value)
                {
                    hideableSection.RemoveFromClassList(HiddenClass);
                }
            }
            
            
            void HideSection()
            {
                if (pinToggle != null && !pinToggle.value)
                {
                    hideableSection.AddToClassList(HiddenClass);
                }
            }

            void ApplyPinnedState(bool pinned)
            {
                //When pinned, disable the revealzone, and show section.
                //When unpinnned, enabled the revealzone, and hide section.
                revealZone?.EnableInClassList(RevealZoneActiveClass, !pinned);
                hideableSection.EnableInClassList(HiddenClass, !pinned);
                hideableSection.EnableInClassList(UnpinnedClass, !pinned);
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
            
            ResolveLinkedElements(hideableSection, out var revealZone, out var pinToggle);

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

        private void ResolveLinkedElements(HideableSection hideableSection, out HideableSectionRevealZone revealZone, out PinToggle pinToggle)
        {
            var root = App.UIRoot.Root;
            revealZone = hideableSection.RevealZoneName != null ? root.Q<HideableSectionRevealZone>(hideableSection.RevealZoneName) : null;
            if (revealZone == null) Debug.LogError("Hideable section's HideableSectionRevealZone could not be found!");
            pinToggle = hideableSection.PinToggleName != null ? root.Q<PinToggle>(hideableSection.PinToggleName) : null;
            if (pinToggle == null) Debug.LogError("Hideable section's PinToggle could not be found!");
        }

    }
}
