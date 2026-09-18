using Netherlands3D.Twin.Functionalities;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Netherlands3D.Services;
using Netherlands3D.Twin.PresentationModus.UIHider;
using Netherlands3D.UI.Components;

namespace Netherlands3D
{
    [RequireComponent(typeof(UIDocument))]
    public class AppRootBehaviour : MonoBehaviour
    {
        public VisualElement Root => appRoot;

        private UIDocument appDocument;
        private VisualElement appRoot;

        private UIHider presentationUIHider;

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
        }

        private void InitializePresentationControls(DefaultHUD defaultHUD, UIHider uiHider)
        {
            presentationUIHider = uiHider;

            presentationUIHider.PresentationChanged += UpdatePresentationControls;

            UpdatePresentationControls();
        }

        private void OnPresentationChanged(ChangeEvent<bool> evt)
        {
            presentationUIHider.SetPresenting(evt.newValue);
            UpdatePresentationControls();
        }

        private void UpdatePresentationControls()
        {
            appRoot.EnableInClassList("app--presenting", presentationUIHider.IsPresenting);
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