using System.Collections.Generic;
using Netherlands3D.Twin;
using Netherlands3D.Twin.PresentationModus;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    public class PresentationModeBehaviour : MonoBehaviour
    {
        [SerializeField] private PresentationModeFunctionality presentationModeFunctionality;
        
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

            presentationModeFunctionality.OnDisableFunctionality
                .AddListener(OnPresentationModeFunctionalityDisabled);

            SetPresentationMode(presentationModeToggles.Count > 0 && presentationModeToggles[0].value);
        }
        
        private void Update()
        {
            if (!presentationModeFunctionality.IsEnabled)
                return;

            if (Keyboard.current?.hKey.wasPressedThisFrame == true)
            {
                var isPresenting = root.ClassListContains(PresentingClass);
                SetPresentationMode(!isPresenting);
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
        
        private void OnPresentationModeFunctionalityDisabled()
        {
            SetPresentationMode(false);
        }

        private void SetPresentationMode(bool isPresenting)
        {
            isPresenting &= presentationModeFunctionality.IsEnabled;
            
            root.EnableInClassList(PresentingClass, isPresenting);

            foreach (var toggle in presentationModeToggles)
            {
                toggle.SetValueWithoutNotify(isPresenting);
            }
        }
        
    }
}