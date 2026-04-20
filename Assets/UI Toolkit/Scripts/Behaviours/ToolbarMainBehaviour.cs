using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    public class ToolbarMainBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument appDocument;

        #region UI Elements
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private HamburgerMenu hamburgerMenu;
        private HamburgerMenu HamburgerMenu => hamburgerMenu ??= Root?.Q<HamburgerMenu>();

        private ToolbarMain toolbarMain;
        private ToolbarMain ToolbarMain => toolbarMain ??= Root?.Q<ToolbarMain>();
        #endregion

        public UnityEvent OnOpenProject = new();
        public UnityEvent OnSaveProject = new();
        public UnityEvent OnOpenSettings = new();
        public UnityEvent OnHelp = new();

        public UnityEvent OnLayer = new();
        public UnityEvent OnLibrary = new();
        public UnityEvent OnAdd = new();
        public UnityEvent OnSearch = new();
        public UnityEvent OnSunPosition = new();
        public UnityEvent OnDownloadTile = new();

        private void OnEnable()
        {
            HamburgerMenu.OpenProjectButton.RegisterCallback<ClickEvent>(OnOpenProjectAction);
            HamburgerMenu.SaveProjectButton.RegisterCallback<ClickEvent>(OnSaveProjectAction);
            HamburgerMenu.SettingsButton.RegisterCallback<ClickEvent>(OnOpenSettingsAction);
            HamburgerMenu.HelpButton.RegisterCallback<ClickEvent>(OnHelpAction);

            ToolbarMain.OnLayerClicked += OnLayerAction;
            ToolbarMain.OnLibraryClicked += OnLibraryAction;
            ToolbarMain.OnAddClicked += OnAddAction;
            ToolbarMain.OnSearchClicked += OnSearchAction;
            ToolbarMain.OnSunPositionClicked += OnSunPositionAction;
            ToolbarMain.OnDownloadTileClicked += OnDownloadTileAction;
        }

        private void OnDisable()
        {
            HamburgerMenu.OpenProjectButton.UnregisterCallback<ClickEvent>(OnOpenProjectAction);
            HamburgerMenu.SaveProjectButton.UnregisterCallback<ClickEvent>(OnSaveProjectAction);
            HamburgerMenu.SettingsButton.UnregisterCallback<ClickEvent>(OnOpenSettingsAction);
            HamburgerMenu.HelpButton.UnregisterCallback<ClickEvent>(OnHelpAction);

            ToolbarMain.OnLayerClicked -= OnLayerAction;
            ToolbarMain.OnLibraryClicked -= OnLibraryAction;
            ToolbarMain.OnAddClicked -= OnAddAction;
            ToolbarMain.OnSearchClicked -= OnSearchAction;
            ToolbarMain.OnSunPositionClicked -= OnSunPositionAction;
            ToolbarMain.OnDownloadTileClicked -= OnDownloadTileAction;
        }

        private void OnOpenProjectAction(ClickEvent _) => OnOpenProject?.Invoke();
        private void OnSaveProjectAction(ClickEvent _) => OnSaveProject?.Invoke();
        private void OnOpenSettingsAction(ClickEvent _) => OnOpenSettings?.Invoke();
        private void OnHelpAction(ClickEvent _) => OnHelp?.Invoke();

        private void OnLayerAction() => OnLayer?.Invoke();
        private void OnLibraryAction() => OnLibrary?.Invoke();
        private void OnAddAction() => OnAdd?.Invoke();
        private void OnSearchAction() => OnSearch?.Invoke();
        private void OnSunPositionAction() => OnSunPosition?.Invoke();
        private void OnDownloadTileAction() => OnDownloadTile?.Invoke();
        
        public void OpenHamburgerMenu() => HamburgerMenu.value = true;
        public void CloseHamburgerMenu() => HamburgerMenu.value = false;
    }
}