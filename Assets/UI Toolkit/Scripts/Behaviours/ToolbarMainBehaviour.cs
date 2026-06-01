using System;
using System.Collections.Generic;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Tools;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI_Toolkit.Scripts.Behaviours
{
    public class ToolbarMainBehaviour : MonoBehaviour
    {
        //[SerializeField] private UIDocument appDocument;
        // [Header("Tools")]
        // [SerializeField] private Tool layerTool;
        // [SerializeField] private Tool assetImportTool;
        // [SerializeField] private Tool assetLibraryTool;
        // [SerializeField] private Tool searchTool;
        // [SerializeField] private Tool sunPositionTool;
        // [SerializeField] private Tool downloadTileTool;
        // [SerializeField] private Tool openProjectTool;
        // [SerializeField] private Tool saveProjectTool;
        // [SerializeField] private Tool settingsTool;
        // [SerializeField] private Tool helpTool;
        
        //#region UI Elements
        // private VisualElement root;
        // private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private HamburgerMenu hamburgerMenu;
        // private HamburgerMenu HamburgerMenu => hamburgerMenu ??= Root?.Q<HamburgerMenu>();

        private ToolbarMain toolbarMain;
        // private ToolbarMain ToolbarMain => toolbarMain ??= Root?.Q<ToolbarMain>();
        //#endregion
        private ToolService tools;

        private void OnEnable()
        {
            hamburgerMenu = App.UIRoot.Root.Q<HamburgerMenu>();
            toolbarMain = App.UIRoot.Root.Q<ToolbarMain>();
            tools = ServiceLocator.GetService<ToolService>();
            
            // hamburgerMenu.OnToolSelected.AddListener(OnToolNotified);
            // tools.onNotifyAny.AddListener(OnToolNotified);
            tools.AnyToolOpened.AddListener(OnAnyToolOpened);
        }

        private void OnAnyToolOpened()
        {
            hamburgerMenu.Close();
        }

        private void OnDisable()
        {
            // hamburgerMenu.OnToolSelected.RemoveListener(OnToolNotified);
            tools.AnyToolOpened.RemoveListener(OnAnyToolOpened);
            // tools.onNotifyAny.RemoveListener(OnToolNotified);
            // tools.onOpenAny.RemoveListener(OnPanelOpen);
        }
        //
        //
        // private void OnEnable()
        // {
        //     // hamburgerMenu.OnOpenProjectSelected += OnOpenProjectSelected;
        //     // hamburgerMenu.OnSaveProjectSelected += OnSaveProjectAction;
        //     // hamburgerMenu.OnSettingsSelected += OnOpenSettingsAction;
        //     // hamburgerMenu.OnHelpSelected += OnHelpAction;
        //     
        //  
        //
        //     // toolbarMain.OnLayerToolSelected += OnLayerToolSelected;
        //     // toolbarMain.OnAddToolSelected += OnAddToolSelected;
        //     // toolbarMain.OnLibraryToolSelected += OnLibraryToolSelected;
        //     // toolbarMain.OnSearchToolSelected += OnSearchToolSelected;
        //     // toolbarMain.OnSunPositionToolSelected += OnSunPositionToolSelected;
        //     // toolbarMain.OnDownloadToolSelected += OnDownloadToolSelected;
        //     //toolbarMain.OnToolDeselected += OnToolDeselected;
        //
        //    
        //         
        //     
        //     // tools.AddOpenListener(ToolType.Layer, OnLayerPanelOpened);
        //     // tools.AddOpenListener(ToolType.AssetImport, OnAssetImportPanelOpened);
        //     // tools.AddOpenListener(ToolType.AssetLibrary, OnAssetLibraryPanelOpened);
        //     // tools.AddOpenListener(ToolType.Search, OnSearchPanelOpened);
        //     // tools.AddOpenListener(ToolType.SunPosition, OnSunPositionPanelOpened);
        //     // tools.AddOpenListener(ToolType.DownloadTile, OnDownloadTilePanelOpened);
        // }
        //
        // private void OnDisable()
        // {
        //     // hamburgerMenu.OnOpenProjectSelected -= OnOpenProjectSelected;
        //     // hamburgerMenu.OnSaveProjectSelected -= OnSaveProjectAction;
        //     // hamburgerMenu.OnSettingsSelected -= OnOpenSettingsAction;
        //     // hamburgerMenu.OnHelpSelected -= OnHelpAction;
        //
        //     
        //     
        //     // toolbarMain.OnLayerToolSelected -= OnLayerToolSelected;
        //     // toolbarMain.OnAddToolSelected -= OnAddToolSelected;
        //     // toolbarMain.OnLibraryToolSelected -= OnLibraryToolSelected;
        //     // toolbarMain.OnSearchToolSelected -= OnSearchToolSelected;
        //     // toolbarMain.OnSunPositionToolSelected -= OnSunPositionToolSelected;
        //     // toolbarMain.OnDownloadToolSelected -= OnDownloadToolSelected;
        //     // toolbarMain.OnToolDeselected -= OnToolDeselected;
        //
        //     
        //
        //     // tools.RemoveOpenListener(ToolType.Layer, OnPanelOpened(ToolType.Layer));
        //     // tools.RemoveOpenListener(ToolType.AssetImport, OnAssetImportPanelOpened);
        //     // tools.RemoveOpenListener(ToolType.AssetLibrary, OnAssetLibraryPanelOpened);
        //     // tools.RemoveOpenListener(ToolType.Search, OnSearchPanelOpened);
        //     // tools.RemoveOpenListener(ToolType.SunPosition, OnSunPositionPanelOpened);
        //     // tools.RemoveOpenListener(ToolType.DownloadTile, OnDownloadTilePanelOpened);
        // }

        // private void OnToolNotified(ToolType toolType)
        // {
        //     hamburgerMenu.Close();
        //     
        //     if(toolType == ToolType.None)
        //         tools.CloseAllTools();
        //     else
        //         tools.OpenTool(toolType);
        // }
        
        private void OnPanelOpen(ToolType toolType)
        {
            hamburgerMenu.Close();
            toolbarMain.EnableToolWithoutNotify(toolType);
        }

        // private void OnLayerToolSelected()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(layerTool);
        // }
        //
        // private void OnAddToolSelected()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(assetImportTool);
        // }
        //
        // private void OnLibraryToolSelected()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(assetLibraryTool);
        // }
        //
        // private void OnSearchToolSelected()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(searchTool);
        // }
        //
        // private void OnSunPositionToolSelected()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(sunPositionTool);
        // }
        //
        // private void OnDownloadToolSelected()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(downloadTileTool);
        // }

        // private void OnToolDeselected()
        // {
        //     HamburgerMenu.Close();
        //     //CloseAllToolbarTools();
        //     
        // }

        // private void OnOpenProjectSelected()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(openProjectTool);
        // }
        //
        // private void OnSaveProjectAction()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(saveProjectTool);
        // }
        //
        // private void OnOpenSettingsAction()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(settingsTool);
        // }
        //
        // private void OnHelpAction()
        // {
        //     HamburgerMenu.Close();
        //     OpenTool(helpTool);
        // }

       
        
        // private void OnAssetLibraryPanelOpened()
        // {
        //     HamburgerMenu.Close();
        //     toolbarMain.EnableToolWithoutNotify(ToolType.AssetLibrary);
        // }
        //
        // private void OnAssetImportPanelOpened()
        // {
        //     HamburgerMenu.Close();
        //     ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.Add);
        // }
        //
        // private void OnLayerPanelOpened()
        // {
        //     HamburgerMenu.Close();
        //     ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.Layer);
        // }
        //
        // private void OnSearchPanelOpened()
        // {
        //     HamburgerMenu.Close();
        //     ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.Search);
        // }
        //
        // private void OnSunPositionPanelOpened()
        // {
        //     HamburgerMenu.Close();
        //     ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.SunPosition);
        // }
        //
        // private void OnDownloadTilePanelOpened()
        // {
        //     HamburgerMenu.Close();
        //     ToolbarMain.EnableToolWithoutNotify(ToolbarMain.Tool.DownloadTile);
        // }

        // private static void OpenTool(Tool tool)
        // {
        //     tool?.OpenInspector();
        // }

        // private static void CloseTool(Tool tool)
        // {
        //     tool?.CloseInspector();
        // }

        // private void CloseAllToolbarTools()
        // {
        //     // CloseTool(layerTool);
        //     // CloseTool(assetImportTool);
        //     // CloseTool(assetLibraryTool);
        //     // CloseTool(searchTool);
        //     // CloseTool(sunPositionTool);
        //     // CloseTool(downloadTileTool);
        //     // CloseTool(openProjectTool);
        //     // CloseTool(saveProjectTool);
        //     // CloseTool(settingsTool);
        //     // CloseTool(helpTool);
        // }
    }
}