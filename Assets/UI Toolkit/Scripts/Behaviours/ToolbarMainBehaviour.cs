using Netherlands3D.Twin.Configuration;
using Netherlands3D.UI.Behaviours;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    public class ToolbarMainBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument appDocument;
        [SerializeField] private InspectorPanelBehaviour inspectorPanelBehaviour;
        
        #region UI Elements
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private HamburgerMenu hamburgerMenu;
        private HamburgerMenu HamburgerMenu => hamburgerMenu ??= Root?.Q<HamburgerMenu>();

        private ToolbarMain toolbarMain;
        private ToolbarMain ToolbarMain => toolbarMain ??= Root?.Q<ToolbarMain>();
        #endregion

        [SerializeField] private ScriptableObject SettingsWindow;
        [SerializeField] private string HelpUrl;

        private void OnEnable()
        {
            HamburgerMenu.OpenProjectButton.RegisterCallback<ClickEvent>(OnOpenProjectSelected);
            HamburgerMenu.SaveProjectButton.RegisterCallback<ClickEvent>(OnSaveProjectAction);
            HamburgerMenu.SettingsButton.RegisterCallback<ClickEvent>(OnOpenSettingsAction);
            HamburgerMenu.HelpButton.RegisterCallback<ClickEvent>(OnHelpAction);

            ToolbarMain.OnLayerToolSelected += OnLayerToolSelected;
            ToolbarMain.OnAddToolSelected += OnAddToolSelected;
            ToolbarMain.OnLibraryToolSelected += OnLibraryToolSelected;
            ToolbarMain.OnSearchToolSelected += OnSearchToolSelected;
            ToolbarMain.OnSunPositionToolSelected += OnSunPositionToolSelected;
            ToolbarMain.OnDownloadToolSelected += OnDownloadToolSelected;
            ToolbarMain.OnToolDeselected += OnToolDeselected;
            
            inspectorPanelBehaviour.AssetLibraryPanelOpened.AddListener(OnAssetLibraryPanelOpened);
            inspectorPanelBehaviour.AssetImportPanelOpened.AddListener(OnAssetImportPanelOpened);
            inspectorPanelBehaviour.LayerPanelOpened.AddListener(OnLayerPanelOpened);
            inspectorPanelBehaviour.SearchPanelOpened.AddListener(OnSearchPanelOpened);
            inspectorPanelBehaviour.SunPositionPanelOpened.AddListener(OnSunPositionPanelOpened);
            inspectorPanelBehaviour.DownloadTilePanelOpened.AddListener(OnDownloadTilePanelOpened);
        }

        private void OnDisable()
        {
            HamburgerMenu.OpenProjectButton.UnregisterCallback<ClickEvent>(OnOpenProjectSelected);
            HamburgerMenu.SaveProjectButton.UnregisterCallback<ClickEvent>(OnSaveProjectAction);
            HamburgerMenu.SettingsButton.UnregisterCallback<ClickEvent>(OnOpenSettingsAction);
            HamburgerMenu.HelpButton.UnregisterCallback<ClickEvent>(OnHelpAction);

            ToolbarMain.OnLayerToolSelected -= OnLayerToolSelected;
            ToolbarMain.OnAddToolSelected -= OnAddToolSelected;
            ToolbarMain.OnLibraryToolSelected -= OnLibraryToolSelected;
            ToolbarMain.OnSearchToolSelected -= OnSearchToolSelected;
            ToolbarMain.OnSunPositionToolSelected -= OnSunPositionToolSelected;
            ToolbarMain.OnDownloadToolSelected -= OnDownloadToolSelected;
            ToolbarMain.OnToolDeselected -= OnToolDeselected;
            
            inspectorPanelBehaviour.AssetLibraryPanelOpened.RemoveListener(OnAssetLibraryPanelOpened);
            inspectorPanelBehaviour.AssetImportPanelOpened.RemoveListener(OnAssetImportPanelOpened);
            inspectorPanelBehaviour.LayerPanelOpened.RemoveListener(OnLayerPanelOpened);
            inspectorPanelBehaviour.SearchPanelOpened.RemoveListener(OnSearchPanelOpened);
            inspectorPanelBehaviour.SunPositionPanelOpened.RemoveListener(OnSunPositionPanelOpened);
            inspectorPanelBehaviour.DownloadTilePanelOpened.RemoveListener(OnDownloadTilePanelOpened);
        }

        private void OnLayerToolSelected()
        {
            HamburgerMenu.Close();
            inspectorPanelBehaviour.OpenLayers();
        }

        private void OnAddToolSelected()
        {
            HamburgerMenu.Close();
            inspectorPanelBehaviour.OpenAssetImport();
        }

        private void OnLibraryToolSelected()
        {
            HamburgerMenu.Close();
            inspectorPanelBehaviour.OpenAssetLibrary();
        }

        private void OnSearchToolSelected()
        {
            HamburgerMenu.Close();
            inspectorPanelBehaviour.OpenSearch();
        }

        private void OnSunPositionToolSelected()
        {
            HamburgerMenu.Close();
            inspectorPanelBehaviour.OpenSunPosition();
        }

        private void OnDownloadToolSelected()
        {
            HamburgerMenu.Close();
            inspectorPanelBehaviour.OpenDownloadTile();
        }

        private void OnToolDeselected()
        {
            HamburgerMenu.Close();
            inspectorPanelBehaviour.Close();
        }

        private void OnOpenProjectSelected(ClickEvent _)
        {
            HamburgerMenu.Close();
            inspectorPanelBehaviour.OpenLoadProject();
        }

        private void OnSaveProjectAction(ClickEvent _)
        {
            HamburgerMenu.Close();
            inspectorPanelBehaviour.OpenSaveProject();
        }

        private void OnOpenSettingsAction(ClickEvent _)
        {
            HamburgerMenu.Close();
            ((IWindow)SettingsWindow).Open();
        }

        private void OnHelpAction(ClickEvent _)
        {
            HamburgerMenu.Close();
            Application.OpenURL(HelpUrl);
        }
        
        private void OnAssetLibraryPanelOpened()
        {
            HamburgerMenu.Close();
            ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.Library);
        }

        private void OnAssetImportPanelOpened()
        {
            HamburgerMenu.Close();
            ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.Add);
        }

        private void OnLayerPanelOpened()
        {
            HamburgerMenu.Close();
            ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.Layer);
        }

        private void OnSearchPanelOpened()
        {
            HamburgerMenu.Close();
            ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.Search);
        }

        private void OnSunPositionPanelOpened()
        {
            HamburgerMenu.Close();
            ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.SunPosition);
        }

        private void OnDownloadTilePanelOpened()
        {
            HamburgerMenu.Close();
            ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.DownloadTile);
        }
    }
}