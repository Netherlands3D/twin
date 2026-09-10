using System;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Projects;
using System.Collections.Generic;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Netherlands3D.Services;
using Netherlands3D.Twin.PresentationModus.UIHider;
using Netherlands3D.UI.Components;
using Netherlands3D.UI_Toolkit.Scripts.Behaviours;

namespace Netherlands3D
{
    [RequireComponent(typeof(UIDocument))]
    public class AppRootBehaviour : MonoBehaviour
    {
        public VisualElement Root => appRoot;

        private UIDocument appDocument;
        private VisualElement appRoot;

        private UIHider presentationUIHider;
        private Netherlands3D.UI.Components.Toggle presentationToggle;
        private ProjectData presentationProject;

        private PresentationPanelHider leftPanelHider;
        private ToolboxPanelHider toolboxPanelHider;
        private PresentationPanelHider navigationPanelHider;
        private PinToggle leftPresentationPin;
        private PinToggle toolboxPresentationPin;
        private PinToggle navigationPresentationPin;

        private bool hasStarted;

        //the excuted order of this script should be executed very early to ensure the presence of the approot. 
        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            appRoot = appDocument?.rootVisualElement;
        }

        private void Start()
        {
            DisableFPVUI();

            hasStarted = true;
            InitializePresentationSections();
        }

        private void OnEnable()
        {
            if (hasStarted)
                InitializePresentationSections();
        }

        private void OnDisable()
        {
            DisposePresentationControls();

            toolboxPanelHider?.Dispose();
            toolboxPanelHider = null;

            navigationPanelHider?.Dispose();
            navigationPanelHider = null;

            leftPanelHider?.Dispose();
            leftPanelHider = null;
        }

        private void InitializePresentationSections()
        {
            if (presentationUIHider != null)
                return;

            var defaultHUD = appRoot.Q<DefaultHUD>();
            var uiHider = ServiceLocator.GetService<UIHider>();

            InitializePresentationControls(defaultHUD, uiHider);

            var leftSection = defaultHUD.Q<VisualElement>("LeftSection");
            var navigationSection = defaultHUD.Q<VisualElement>("NavigationSection");
            var presentationControls = defaultHUD.Q<VisualElement>("PresentationControls");
            var toolbox = defaultHUD.Q<ToolbarToolbox>();
            var scenario = defaultHUD.Q<ToolbarScenario>();

            leftPanelHider = new PresentationPanelHider(uiHider, leftSection, leftPresentationPin, UIExitDirection.Left);
            navigationPanelHider = new PresentationPanelHider(uiHider, navigationSection, navigationPresentationPin, UIExitDirection.Down, releaseHorizontalSpace: true, hoverExclusion: presentationControls);
            toolboxPanelHider = new ToolboxPanelHider(uiHider, toolbox, scenario);
        }

        private void InitializePresentationControls(DefaultHUD defaultHUD, UIHider uiHider)
        {
            presentationUIHider = uiHider;
            presentationToggle = defaultHUD.Q<Netherlands3D.UI.Components.Toggle>("Presentation");
            leftPresentationPin = defaultHUD.Q<PinToggle>("LeftPresentationPin");
            toolboxPresentationPin = defaultHUD.Q<PinToggle>("PresentationPin");
            navigationPresentationPin = defaultHUD.Q<PinToggle>("NavigationPresentationPin");
            presentationProject = ProjectData.Current;

            RestorePresentationPins(presentationProject);

            presentationToggle.RegisterValueChangedCallback(OnPresentationChanged);
            leftPresentationPin.RegisterValueChangedCallback(OnPresentationPinChanged);
            toolboxPresentationPin.RegisterValueChangedCallback(OnPresentationPinChanged);
            navigationPresentationPin.RegisterValueChangedCallback(OnPresentationPinChanged);
            presentationUIHider.PresentationChanged += UpdatePresentationControls;
            presentationProject.OnDataChanged.AddListener(OnPresentationProjectChanged);

            UpdatePresentationControls();
        }

        private void OnPresentationChanged(ChangeEvent<bool> evt)
        {
            presentationUIHider.SetPresenting(evt.newValue);
            UpdatePresentationControls();
        }

        private void UpdatePresentationControls()
        {
            presentationToggle.SetValueWithoutNotify(presentationUIHider.IsPresenting);
            appRoot.EnableInClassList("app--presenting", presentationUIHider.IsPresenting);
        }

        private void OnPresentationPinChanged(ChangeEvent<bool> evt)
        {
            presentationProject.PresentationLeftPinned = leftPresentationPin.value;
            presentationProject.PresentationToolboxPinned = toolboxPresentationPin.value;
            presentationProject.PresentationNavigationPinned = navigationPresentationPin.value;
        }

        private void RestorePresentationPins(ProjectData project)
        {
            leftPresentationPin.SetValueWithoutNotify(project.PresentationLeftPinned);
            toolboxPresentationPin.SetValueWithoutNotify(project.PresentationToolboxPinned);
            navigationPresentationPin.SetValueWithoutNotify(project.PresentationNavigationPinned);
        }

        private void OnPresentationProjectChanged(ProjectData project)
        {
            RestorePresentationPins(project);

            if (presentationUIHider.IsPresenting)
                presentationUIHider.SetPresenting(false);
            else
                presentationUIHider.RefreshPanels();
        }

        private void DisposePresentationControls()
        {
            if (presentationUIHider == null)
                return;

            presentationToggle.UnregisterValueChangedCallback(OnPresentationChanged);
            leftPresentationPin.UnregisterValueChangedCallback(OnPresentationPinChanged);
            toolboxPresentationPin.UnregisterValueChangedCallback(OnPresentationPinChanged);
            navigationPresentationPin.UnregisterValueChangedCallback(OnPresentationPinChanged);
            presentationUIHider.PresentationChanged -= UpdatePresentationControls;
            presentationProject.OnDataChanged.RemoveListener(OnPresentationProjectChanged);

            presentationUIHider = null;
            presentationToggle = null;
            leftPresentationPin = null;
            toolboxPresentationPin = null;
            navigationPresentationPin = null;
            presentationProject = null;
        }

        //todo: in the future we might want to create a list of huds we can switch between, so we avoid multiple true/false permutations, but for now we only have 2, so this is not needed yet
        public void DisableFPVUI()
        {
            appRoot.Q<DefaultHUD>().EnableInClassList(UtilityClassConstants.HIDDEN, false);
            appRoot.Q<FPVHUD>().EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }

        public void EnableFPVUI()
        {
            appRoot.Q<FPVHUD>().EnableInClassList(UtilityClassConstants.HIDDEN, false);
            appRoot.Q<DefaultHUD>().EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }

        /// <summary>
        /// Some UI elements should behave differently (i.e. be shown) when a functionality is enabled. This code
        /// will add a class on the top-most level so that each component can decide how to respond when a functionality
        /// is enabled.
        /// </summary>
        public void EnableFunctionality(Functionality functionality)
        {
            appRoot.AddToClassList("app--functionality-" + functionality.Id);

            if (functionality.Id == UIHider.FunctionalityId)
                ServiceLocator.GetService<UIHider>().SetPresentationEnabled(true);
        }

        /// <summary>
        /// Removes the global class that allows part of the application's UI to respond to the functionality being
        /// disabled.
        /// </summary>
        public void DisableFunctionality(Functionality functionality)
        {
            appRoot.RemoveFromClassList("app--functionality-" + functionality.Id);

            if (functionality.Id == UIHider.FunctionalityId)
                ServiceLocator.GetService<UIHider>().SetPresentationEnabled(false);
        }

        public Vector2 GetPanelClickPosition()
        {
            var screenPos = Pointer.current.position.ReadValue();
            screenPos.y = Screen.height - screenPos.y;
            return RuntimePanelUtils.ScreenToPanel(appRoot.panel, screenPos);
        }

        public Vector2 GetUIPositionFromScreenPosition(Vector2 screenPos)
        {
            screenPos.y = Screen.height - screenPos.y;
            return RuntimePanelUtils.ScreenToPanel(appRoot.panel, screenPos);
        }

        public bool IsPointerOverUI()
        {
            return IsPointerOverUI(out _);
        }
        
        public bool IsPointerOverUI(out VisualElement picked)
        {
            Vector2 panelPosition = GetPanelClickPosition();
            picked = appRoot.panel.Pick(panelPosition);
            return picked != null;
        }
    }
}