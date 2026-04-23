using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Credentials;
using Netherlands3D.Twin.Tools;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
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

        [Header("Deprecated tools until migration is completed")]
        [SerializeField] private Tool Layer;
        [SerializeField] private GameObject Search;
        [SerializeField] private Tool SunPosition;
        [SerializeField] private Tool DownloadTile;
        [SerializeField] private Tool OpenProject;
        [SerializeField] private Tool SaveProject;
        
        [Header("Events")]
        public UnityEvent AssetLibraryPanelOpened = new();
        public UnityEvent AssetImportPanelOpened = new();
        public UnityEvent LayerPanelOpened = new();
        public UnityEvent SearchPanelOpened = new();
        public UnityEvent SunPositionPanelOpened = new();
        public UnityEvent DownloadTilePanelOpened = new();
        public UnityEvent OpenProjectPanelOpened = new();
        public UnityEvent SaveProjectPanelOpened = new();

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
            ImportAssetPanel.OpenAssetLibrary += OpenAssetLibrary;
            ImportAssetPanel.importSucceeded.AddListener(OnImportSucceeded);
        }

        private void OnDisable()
        {
            InspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            InspectorPanel.InspectorHeaderCloseButton.clicked -= Close;
            ImportAssetPanel.OpenAssetLibrary -= OpenAssetLibrary;
            ImportAssetPanel.importSucceeded.RemoveListener(OnImportSucceeded);
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

        public void OpenAssetLibrary()
        {
            ShowPanel<AssetLibraryPanel>();
            AssetLibraryPanelOpened.Invoke();
        }

        public void OpenAssetImport()
        {
            ShowPanel<ImportAssetPanel>();
            AssetImportPanelOpened.Invoke();
        }

        public void OpenLayers()
        {
            OpenTool(Layer);
            LayerPanelOpened.Invoke();
        }

        public void OpenSearch()
        {
            Close();
            Search.SetActive(true);
            SearchPanelOpened.Invoke();
        }

        public void OpenSunPosition()
        {
            OpenTool(SunPosition);
            SunPositionPanelOpened.Invoke();
        }

        public void OpenDownloadTile()
        {
            OpenTool(DownloadTile);
            DownloadTilePanelOpened.Invoke();
        }

        public void OpenLoadProject()
        {
            OpenTool(OpenProject);
            OpenProjectPanelOpened.Invoke();
        }
        
        public void OpenSaveProject()
        {
            OpenTool(SaveProject);
            SaveProjectPanelOpened.Invoke();
        }

        [Obsolete("Tools are considered obsolete, as soon as we have migrated all panels this van be deleted")]
        private void OpenTool(Tool tool)
        {
            Close();
            tool.OpenInspector();
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
            if (evt.newValue) ShowPanel<ImportAssetPanel>();
            else Close();
        }

        private void OnOpenLibraryToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue) ShowPanel<AssetLibraryPanel>();
            else Close();
        }

        private void OnImportSucceeded()
        {
            InspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(false);
            Close();
        }
    }
}
