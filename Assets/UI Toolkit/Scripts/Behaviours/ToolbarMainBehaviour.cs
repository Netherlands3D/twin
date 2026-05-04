using Netherlands3D.Twin.Tools;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    public class ToolbarMainBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument appDocument;
        [Header("Tools")]
        [SerializeField] private Tool layerTool;
        [SerializeField] private Tool assetImportTool;
        [SerializeField] private Tool assetLibraryTool;
        [SerializeField] private Tool searchTool;
        [SerializeField] private Tool sunPositionTool;
        [SerializeField] private Tool downloadTileTool;
        [SerializeField] private Tool openProjectTool;
        [SerializeField] private Tool saveProjectTool;
        [SerializeField] private Tool settingsTool;
        [SerializeField] private Tool helpTool;
        
        #region UI Elements
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private HamburgerMenu hamburgerMenu;
        private HamburgerMenu HamburgerMenu => hamburgerMenu ??= Root?.Q<HamburgerMenu>();

        private ToolbarMain toolbarMain;
        private ToolbarMain ToolbarMain => toolbarMain ??= Root?.Q<ToolbarMain>();
        #endregion

        private void OnEnable()
        {
            HamburgerMenu.OnOpenProjectSelected += OnOpenProjectSelected;
            HamburgerMenu.OnSaveProjectSelected += OnSaveProjectAction;
            HamburgerMenu.OnSettingsSelected += OnOpenSettingsAction;
            HamburgerMenu.OnHelpSelected += OnHelpAction;

            ToolbarMain.OnLayerToolSelected += OnLayerToolSelected;
            ToolbarMain.OnAddToolSelected += OnAddToolSelected;
            ToolbarMain.OnLibraryToolSelected += OnLibraryToolSelected;
            ToolbarMain.OnSearchToolSelected += OnSearchToolSelected;
            ToolbarMain.OnSunPositionToolSelected += OnSunPositionToolSelected;
            ToolbarMain.OnDownloadToolSelected += OnDownloadToolSelected;
            ToolbarMain.OnToolDeselected += OnToolDeselected;

            AddOpenListener(layerTool, OnLayerPanelOpened);
            AddOpenListener(assetImportTool, OnAssetImportPanelOpened);
            AddOpenListener(assetLibraryTool, OnAssetLibraryPanelOpened);
            AddOpenListener(searchTool, OnSearchPanelOpened);
            AddOpenListener(sunPositionTool, OnSunPositionPanelOpened);
            AddOpenListener(downloadTileTool, OnDownloadTilePanelOpened);
        }

        private void OnDisable()
        {
            HamburgerMenu.OnOpenProjectSelected -= OnOpenProjectSelected;
            HamburgerMenu.OnSaveProjectSelected -= OnSaveProjectAction;
            HamburgerMenu.OnSettingsSelected -= OnOpenSettingsAction;
            HamburgerMenu.OnHelpSelected -= OnHelpAction;

            ToolbarMain.OnLayerToolSelected -= OnLayerToolSelected;
            ToolbarMain.OnAddToolSelected -= OnAddToolSelected;
            ToolbarMain.OnLibraryToolSelected -= OnLibraryToolSelected;
            ToolbarMain.OnSearchToolSelected -= OnSearchToolSelected;
            ToolbarMain.OnSunPositionToolSelected -= OnSunPositionToolSelected;
            ToolbarMain.OnDownloadToolSelected -= OnDownloadToolSelected;
            ToolbarMain.OnToolDeselected -= OnToolDeselected;

            RemoveOpenListener(layerTool, OnLayerPanelOpened);
            RemoveOpenListener(assetImportTool, OnAssetImportPanelOpened);
            RemoveOpenListener(assetLibraryTool, OnAssetLibraryPanelOpened);
            RemoveOpenListener(searchTool, OnSearchPanelOpened);
            RemoveOpenListener(sunPositionTool, OnSunPositionPanelOpened);
            RemoveOpenListener(downloadTileTool, OnDownloadTilePanelOpened);
        }

        private void OnLayerToolSelected()
        {
            HamburgerMenu.Close();
            OpenTool(layerTool);
        }

        private void OnAddToolSelected()
        {
            HamburgerMenu.Close();
            OpenTool(assetImportTool);
        }

        private void OnLibraryToolSelected()
        {
            HamburgerMenu.Close();
            OpenTool(assetLibraryTool);
        }

        private void OnSearchToolSelected()
        {
            HamburgerMenu.Close();
            OpenTool(searchTool);
        }

        private void OnSunPositionToolSelected()
        {
            HamburgerMenu.Close();
            OpenTool(sunPositionTool);
        }

        private void OnDownloadToolSelected()
        {
            HamburgerMenu.Close();
            OpenTool(downloadTileTool);
        }

        private void OnToolDeselected()
        {
            HamburgerMenu.Close();
            CloseAllToolbarTools();
        }

        private void OnOpenProjectSelected()
        {
            HamburgerMenu.Close();
            OpenTool(openProjectTool);
        }

        private void OnSaveProjectAction()
        {
            HamburgerMenu.Close();
            OpenTool(saveProjectTool);
        }

        private void OnOpenSettingsAction()
        {
            HamburgerMenu.Close();
            OpenTool(settingsTool);
        }

        private void OnHelpAction()
        {
            HamburgerMenu.Close();
            OpenTool(helpTool);
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

        private static void OpenTool(Tool tool)
        {
            tool?.OpenInspector();
        }

        private static void CloseTool(Tool tool)
        {
            tool?.CloseInspector();
        }

        private void CloseAllToolbarTools()
        {
            CloseTool(layerTool);
            CloseTool(assetImportTool);
            CloseTool(assetLibraryTool);
            CloseTool(searchTool);
            CloseTool(sunPositionTool);
            CloseTool(downloadTileTool);
            CloseTool(openProjectTool);
            CloseTool(saveProjectTool);
            CloseTool(settingsTool);
            CloseTool(helpTool);
        }

        private static void AddOpenListener(Tool tool, UnityEngine.Events.UnityAction listener)
        {
            if (tool == null) return;
            tool.onOpen.AddListener(listener);
        }

        private static void RemoveOpenListener(Tool tool, UnityEngine.Events.UnityAction listener)
        {
            if (tool == null) return;
            tool.onOpen.RemoveListener(listener);
        }
    }
}