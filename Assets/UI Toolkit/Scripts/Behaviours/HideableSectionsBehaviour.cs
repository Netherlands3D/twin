using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.HideableSections;
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

        [SerializeField] private HideableSectionsFunctionality hideableSectionsFunctionality;

        private readonly Dictionary<HideableSection, SectionRegistration> registrations = new();
        private HideableSectionsFunctionalityData functionalityData;
        
        private VisualElement root;

        private void OnEnable()
        {
           
            root = App.UIRoot.Root;

            root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            root.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RegisterHideableSections();
        }

        private void OnDisable()
        {
            if (root != null)
            {
                root.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                root.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            UnregisterHideableSections();
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
            functionalityData = hideableSectionsFunctionality.Data;
            
            var sections = root.Query<HideableSection>().ToList();

            foreach (var section in sections)
            {
                RegisterHideableSection(section);
            }

            hideableSectionsFunctionality.OnDataReplaced.AddListener(HandleFunctionalityDataReplaced);
        }

        private void RegisterHideableSection(HideableSection section)
        {
            if (registrations.ContainsKey(section))
            {
                return;
            }

            var revealZone = FindLinkedElement<HideableSectionRevealZone>(section.RevealZoneName, section);
            var pinToggle = FindLinkedElement<PinToggle>(section.PinToggleName, section);

            var registration = new SectionRegistration(section, revealZone, pinToggle);
            registrations.Add(section, registration);

            RegisterCallbacks(registration);
            ApplyInitialPinState(registration);
        }

        private void RegisterCallbacks(SectionRegistration registration)
        {
            registration.SectionEnterCallback = _ => ShowSection(registration, hideableSectionsFunctionality.Data);
            registration.Section.RegisterCallback(registration.SectionEnterCallback);
            
            registration.SectionLeaveCallback = _ => HideSection(registration, hideableSectionsFunctionality.Data);
            registration.Section.RegisterCallback(registration.SectionLeaveCallback);

            if (registration.RevealZone != null) {
                
                registration.RevealZoneEnterCallback = _ => ShowSection(registration, hideableSectionsFunctionality.Data);
                registration.RevealZone.RegisterCallback(registration.RevealZoneEnterCallback);
                
                registration.RevealZoneLeaveCallback = _ => HideSection(registration, hideableSectionsFunctionality.Data);
                registration.RevealZone.RegisterCallback(registration.RevealZoneLeaveCallback);
            }

            if (registration.PinToggle != null)
            {
                registration.PinChangedCallback = evt => OnPinToggleChanged(registration, evt.newValue);
                registration.PinToggle.RegisterValueChangedCallback(registration.PinChangedCallback);
            }

            registration.PinStateChangedCallback = (sectionId, pinState) => OnPinStateChanged(registration, sectionId, pinState);
            functionalityData.PinStateChanged += registration.PinStateChangedCallback;
        }

        private void ApplyInitialPinState(SectionRegistration registration)
        {
            var isPinned = hideableSectionsFunctionality.Data.IsPinned(registration.SectionId);
            ApplyPinnedState(registration, isPinned);
        }

        private void OnPinToggleChanged(SectionRegistration registration, bool isPinned)
        {
            hideableSectionsFunctionality.Data.SetPinned(registration.SectionId, isPinned);
        }

        private void OnPinStateChanged(SectionRegistration registration, string sectionId, bool isPinned) {

            if (registration.SectionId == sectionId)
            {
                ApplyPinnedState(registration, isPinned);
            }
        }

        private void ApplyPinnedState(SectionRegistration registration, bool isPinned)
        {
            registration.PinToggle?.SetValueWithoutNotify(isPinned);
            registration.RevealZone?.EnableInClassList(RevealZoneActiveClass, !isPinned);
            registration.Section.EnableInClassList(HiddenClass, !isPinned);
            registration.Section.EnableInClassList(UnpinnedClass, !isPinned);
        }

        private static void ShowSection(SectionRegistration registration, HideableSectionsFunctionalityData data)
        {
            if (!data.IsPinned(registration.SectionId))
            {
                registration.Section.RemoveFromClassList(HiddenClass);
            }
        }

        private static void HideSection(SectionRegistration registration, HideableSectionsFunctionalityData data)
        {
            if (!data.IsPinned(registration.SectionId))
            {
                registration.Section.AddToClassList(HiddenClass);
            }
        }

        private void UnregisterHideableSections()
        {
            foreach (var registration in registrations.Values.ToArray())
            {
                UnregisterHideableSection(registration);
            }
            
            hideableSectionsFunctionality.OnDataReplaced.RemoveListener(HandleFunctionalityDataReplaced);
        }

        private void UnregisterHideableSection(
            SectionRegistration registration)
        {
            registration.Section.UnregisterCallback(registration.SectionEnterCallback);
            registration.Section.UnregisterCallback(registration.SectionLeaveCallback);

            if (registration.RevealZone != null)
            {
                registration.RevealZone.UnregisterCallback(registration.RevealZoneEnterCallback);
                registration.RevealZone.UnregisterCallback(registration.RevealZoneLeaveCallback);
            }

            if (registration.PinToggle != null) {
                registration.PinToggle.UnregisterValueChangedCallback(registration.PinChangedCallback);
            }

            functionalityData.PinStateChanged -= registration.PinStateChangedCallback;

            registrations.Remove(registration.Section);
        }

        private T FindLinkedElement<T>(string elementName, HideableSection section) where T : VisualElement
        {
            var element = string.IsNullOrWhiteSpace(elementName) ? null : root.Q<T>(elementName);

            if (element == null)
            {
                Debug.LogError($"Could not find {typeof(T)} '{elementName}' for hideable section '{section.name}'.", this);
            }

            return element;
        }
        
        private void HandleFunctionalityDataReplaced(FunctionalityData previous, FunctionalityData current)
        {
            UnregisterHideableSections();
            RegisterHideableSections();
        }

        private sealed class SectionRegistration
        {
            public HideableSection Section { get; }
            public HideableSectionRevealZone RevealZone { get; }
            public PinToggle PinToggle { get; }

            public string SectionId => Section.SectionId;

            public EventCallback<PointerEnterEvent> SectionEnterCallback { get; set; }
            public EventCallback<PointerLeaveEvent> SectionLeaveCallback { get; set; }

            public EventCallback<PointerEnterEvent> RevealZoneEnterCallback { get; set; }
            public EventCallback<PointerLeaveEvent> RevealZoneLeaveCallback { get; set; }

            public EventCallback<ChangeEvent<bool>> PinChangedCallback { get; set; }
            
            public Action<string, bool> PinStateChangedCallback { get; set; }

            public SectionRegistration(HideableSection section, HideableSectionRevealZone revealZone, PinToggle pinToggle)
            {
                Section = section;
                RevealZone = revealZone;
                PinToggle = pinToggle;
            }
        }
    }
}