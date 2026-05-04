using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Credentials;
using Netherlands3D.Twin.Tools;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument), typeof(ICredentialHandler))]
    public class InspectorPanelBehaviour : MonoBehaviour
    {
        private UIDocument appDocument;
        [SerializeField] private AssetLibrary.AssetLibrary assetLibrary;
    
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private InspectorPanel inspectorPanel;
        private InspectorPanel InspectorPanel => inspectorPanel ??= Root?.Q<InspectorPanel>();

        private AssetLibraryPanel assetLibraryPanel;
        private AssetLibraryPanel AssetLibraryPanel => assetLibraryPanel ??= panels.OfType<AssetLibraryPanel>().FirstOrDefault();
        
        private ImportAssetPanel importAssetPanel;
        private ImportAssetPanel ImportAssetPanel => importAssetPanel ??= panels.OfType<ImportAssetPanel>().FirstOrDefault();

        private readonly HashSet<BaseInspectorContentPanel> panels = new();
        private BaseInspectorContentPanel activePanel;
        
        private ToolbarMain toolbarMain;
        private ToolbarMain ToolbarMain => toolbarMain ??= Root?.Q<ToolbarMain>();
        
        private ICredentialHandler credentialHandler;

        [Header("Tools")]
        [SerializeField] private Tool AssetLibrary;
        [SerializeField] private Tool AssetImport;
        [SerializeField] private Tool Layer;
        [SerializeField] private Tool SearchTool;
        [SerializeField] private GameObject Search;
        [SerializeField] private Tool SunPosition;
        [SerializeField] private Tool DownloadTile;
        [SerializeField] private Tool OpenProject;
        [SerializeField] private Tool SaveProject;

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            credentialHandler = GetComponent<ICredentialHandler>();
            RegisterPanel<AssetLibraryPanel>(assetLibrary);
            RegisterPanel<ImportAssetPanel>();
            
            InspectorPanel.Close();
            
            ImportAssetPanel.SetCredentialHandler(credentialHandler);
        }

        private void OnEnable()
        {
            InspectorPanel.Toolbar.OnAddLayerToggled += OnAddLayerToggled;
            InspectorPanel.Toolbar.OnOpenLibraryToggled += OnOpenLibraryToggled;
            InspectorPanel.InspectorHeaderCloseButton.clicked += Close;
            ImportAssetPanel.OpenAssetLibrary += OnOpenAssetLibraryClicked;
            ImportAssetPanel.importSucceeded.AddListener(OnImportSucceeded);

            AddOpenListener(AssetLibrary, OnAssetLibraryToolOpened);
            AddOpenListener(AssetImport, OnAssetImportToolOpened);
            AddOpenListener(Layer, OnLayerToolOpened);
            AddOpenListener(SearchTool, OnSearchToolOpened);
            AddOpenListener(SunPosition, OnSunPositionToolOpened);
            AddOpenListener(DownloadTile, OnDownloadTileToolOpened);
            AddOpenListener(OpenProject, OnOpenProjectToolOpened);
            AddOpenListener(SaveProject, OnSaveProjectToolOpened);

            AddCloseListener(AssetLibrary, OnToolClosed);
            AddCloseListener(AssetImport, OnToolClosed);
            AddCloseListener(Layer, OnToolClosed);
            AddCloseListener(SearchTool, OnToolClosed);
            AddCloseListener(SunPosition, OnToolClosed);
            AddCloseListener(DownloadTile, OnToolClosed);
            AddCloseListener(OpenProject, OnToolClosed);
            AddCloseListener(SaveProject, OnToolClosed);
        }

        private void OnDisable()
        {
            InspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            InspectorPanel.InspectorHeaderCloseButton.clicked -= Close;
            ImportAssetPanel.OpenAssetLibrary -= OnOpenAssetLibraryClicked;
            ImportAssetPanel.importSucceeded.RemoveListener(OnImportSucceeded);

            RemoveOpenListener(AssetLibrary, OnAssetLibraryToolOpened);
            RemoveOpenListener(AssetImport, OnAssetImportToolOpened);
            RemoveOpenListener(Layer, OnLayerToolOpened);
            RemoveOpenListener(SearchTool, OnSearchToolOpened);
            RemoveOpenListener(SunPosition, OnSunPositionToolOpened);
            RemoveOpenListener(DownloadTile, OnDownloadTileToolOpened);
            RemoveOpenListener(OpenProject, OnOpenProjectToolOpened);
            RemoveOpenListener(SaveProject, OnSaveProjectToolOpened);

            RemoveCloseListener(AssetLibrary, OnToolClosed);
            RemoveCloseListener(AssetImport, OnToolClosed);
            RemoveCloseListener(Layer, OnToolClosed);
            RemoveCloseListener(SearchTool, OnToolClosed);
            RemoveCloseListener(SunPosition, OnToolClosed);
            RemoveCloseListener(DownloadTile, OnToolClosed);
            RemoveCloseListener(OpenProject, OnToolClosed);
            RemoveCloseListener(SaveProject, OnToolClosed);
        }

        public void Open()
        {
            InspectorPanel.Open();
        }

        public void Close()
        {
            // TODO: Remove as soon as search is implemented as a panel
            Search.SetActive(false);
            
            ToolbarMain.ClearWithoutNotify();
            InspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify();
            InspectorPanel.Close();
        }

        // TODO: Shouldn't this be in the InspectorPanel component?
        private BaseInspectorContentPanel RegisterPanel<T>(params object[] args) where T : BaseInspectorContentPanel
        {
            var panel = (T)Activator.CreateInstance(typeof(T), args);
            panels.Add(panel);

            InspectorPanel.Content.Add(panel);
            panel.Hide();

            return panel;
        }

        private void OnAssetLibraryToolOpened()
        {
            CloseAllTrackedToolsExcept(AssetLibrary);
            ShowPanel<AssetLibraryPanel>();
        }

        private void OnAssetImportToolOpened()
        {
            CloseAllTrackedToolsExcept(AssetImport);
            ShowPanel<ImportAssetPanel>();
        }

        private void OnLayerToolOpened()
        {
            CloseAllTrackedToolsExcept(Layer);
            CloseInspectorForExternalTool();
        }

        private void OnSearchToolOpened()
        {
            CloseAllTrackedToolsExcept(SearchTool);
            CloseInspectorForExternalTool();
            Search.SetActive(true);
        }

        private void OnSunPositionToolOpened()
        {
            CloseAllTrackedToolsExcept(SunPosition);
            CloseInspectorForExternalTool();
        }

        private void OnDownloadTileToolOpened()
        {
            CloseAllTrackedToolsExcept(DownloadTile);
            CloseInspectorForExternalTool();
        }

        private void OnOpenProjectToolOpened()
        {
            CloseAllTrackedToolsExcept(OpenProject);
            CloseInspectorForExternalTool();
        }

        private void OnSaveProjectToolOpened()
        {
            CloseAllTrackedToolsExcept(SaveProject);
            CloseInspectorForExternalTool();
        }

        // External tools should close the UI Toolkit inspector without clearing the selected main toolbar button.
        private void CloseInspectorForExternalTool()
        {
            Search.SetActive(false);
            HidePanel();
            InspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify();
            InspectorPanel.Close();
        }

        private void ShowPanel<T>() where T : BaseInspectorContentPanel
        {
            // only one panel can be open at a time
            HidePanel();
            Open();
            activePanel = GetPanel<T>();
            InspectorPanel.HeaderText = activePanel.GetTitle();
            InspectorPanel.ToolbarStyle = activePanel.ToolbarStyle;
            activePanel.Show();
        }

        private T GetPanel<T>() where T : BaseInspectorContentPanel
        {
            return panels.OfType<T>().FirstOrDefault();
        }

        private void HidePanel()
        {
            activePanel?.Hide();
            activePanel = null;
        }

        private void OnAddLayerToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue) AssetImport?.OpenInspector();
            else AssetImport?.CloseInspector();
        }

        private void OnOpenLibraryToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue) AssetLibrary?.OpenInspector();
            else AssetLibrary?.CloseInspector();
        }

        private void OnOpenAssetLibraryClicked()
        {
            AssetLibrary?.OpenInspector();
        }

        private void OnToolClosed()
        {
            if (!AnyToolOpen())
            {
                Close();
            }
        }

        private bool AnyToolOpen()
        {
            return (AssetLibrary != null && AssetLibrary.Open)
                   || (AssetImport != null && AssetImport.Open)
                   || (Layer != null && Layer.Open)
                   || (SearchTool != null && SearchTool.Open)
                   || (SunPosition != null && SunPosition.Open)
                   || (DownloadTile != null && DownloadTile.Open)
                   || (OpenProject != null && OpenProject.Open)
                   || (SaveProject != null && SaveProject.Open);
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

        private static void AddCloseListener(Tool tool, UnityEngine.Events.UnityAction listener)
        {
            if (tool == null) return;
            tool.onClose.AddListener(listener);
        }

        private static void RemoveCloseListener(Tool tool, UnityEngine.Events.UnityAction listener)
        {
            if (tool == null) return;
            tool.onClose.RemoveListener(listener);
        }

        private void OnImportSucceeded()
        {
            InspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(false);
            Close();
        }

        /// <summary>
        /// Closes all tracked tools except the one that just opened,
        /// so the inspector panel lifecycle stays consistent during tool switches.
        /// Safe to call from within an onOpen handler: the exception tool is already
        /// Open=true, so AnyToolOpen() remains true and Close() is not triggered.
        /// </summary>
        private void CloseAllTrackedToolsExcept(Tool exception)
        {
            CloseTrackedTool(AssetLibrary, exception);
            CloseTrackedTool(AssetImport, exception);
            CloseTrackedTool(Layer, exception);
            CloseTrackedTool(SearchTool, exception);
            CloseTrackedTool(SunPosition, exception);
            CloseTrackedTool(DownloadTile, exception);
            CloseTrackedTool(OpenProject, exception);
            CloseTrackedTool(SaveProject, exception);
        }

        private static void CloseTrackedTool(Tool tool, Tool exception)
        {
            if (tool != null && tool != exception)
                tool.CloseInspector();
        }
    }
}
