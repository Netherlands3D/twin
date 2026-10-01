using System.Collections.Generic;
using Netherlands3D.Twin;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    public class PresentationModeBehaviour : MonoBehaviour
    {
        private const string PresentingClass = "presenting-mode--presenting";
        
        private List<PresentationModeToggle> presentationModeToggles;
        private VisualElement root;

        private void Awake()
        {
            root = App.UIRoot.Root;
            presentationModeToggles = root.Query<PresentationModeToggle>().ToList();
        }

        private void OnEnable()
        {
            foreach (var toggle in presentationModeToggles)
            {
                toggle.RegisterValueChangedCallback(OnPresentationModeChanged);
            }

            if (presentationModeToggles.Count > 0)
            {
                SetPresentationMode(presentationModeToggles[0].value);
            }
        }

        private void OnDisable()
        {
            foreach (var toggle in presentationModeToggles)
            {
                toggle.UnregisterValueChangedCallback(OnPresentationModeChanged);
            }
        }

        private void OnPresentationModeChanged(ChangeEvent<bool> evt)
        {
            SetPresentationMode(evt.newValue);
        }

        private void SetPresentationMode(bool isPresenting)
        {
            root.EnableInClassList(PresentingClass, isPresenting);

            foreach (var toggle in presentationModeToggles)
            {
                toggle.SetValueWithoutNotify(isPresenting);
            }
        }
    }
}