using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Credentials;
using Netherlands3D.Events;
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
        
        private InspectorPolygonGridPanel polygonGridPanel;
        private InspectorPolygonGridPanel PolygonGridPanel => polygonGridPanel ??= panels.OfType<InspectorPolygonGridPanel>().FirstOrDefault();

        private readonly HashSet<BaseInspectorContentPanel> panels = new();
        private BaseInspectorContentPanel activePanel;
        
        private ToolbarMain toolbarMain;
        private ToolbarMain ToolbarMain => toolbarMain ??= Root?.Q<ToolbarMain>();
        
        private ICredentialHandler credentialHandler;

        [SerializeField] private TriggerEvent OnDrawNewGrid;
        

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            credentialHandler = GetComponent<ICredentialHandler>();
            RegisterPanel<AssetLibraryPanel>(assetLibrary);
            RegisterPanel<ImportAssetPanel>();
            RegisterPanel<InspectorPolygonGridPanel>();
            
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
            
            ToolbarMain.AddButton.clicked += ToggleImportAssetPanel;
            
            OnDrawNewGrid.AddListenerStarted(OpenPolgyonGridPanel);
        }

        private void OnDisable()
        {
            InspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            InspectorPanel.InspectorHeaderCloseButton.clicked -= Close;
            ImportAssetPanel.OpenAssetLibrary -= OpenAssetLibrary;
            ImportAssetPanel.importSucceeded.RemoveListener(OnImportSucceeded);
            
            ToolbarMain.AddButton.clicked -= ToggleImportAssetPanel;
            
            OnDrawNewGrid.RemoveListenerStarted(OpenPolgyonGridPanel);
        }

        public void Open()
        {
            InspectorPanel.Open();
        }

        public void Close()
        {
            ToolbarMain.ClearWithoutNotify();
            InspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify();
            InspectorPanel.Close();
        }

        // TODO: Shouldn't this be in the InspectorPanel component?
        public BaseInspectorContentPanel RegisterPanel<T>(params object[] args) where T : BaseInspectorContentPanel
        {
            var panel = (T)Activator.CreateInstance(typeof(T), args);
            panels.Add(panel);

            InspectorPanel.Content.Add(panel);
            panel.Hide();

            return panel;
        }

        public void ShowPanel<T>() where T : BaseInspectorContentPanel
        {
            // only one panel can be open at a time
            HidePanel();
            Open();
            activePanel = GetPanel<T>();
            InspectorPanel.HeaderText = activePanel.Title;
            InspectorPanel.ToolbarStyle = activePanel.ToolbarStyle;
            activePanel.Show();
        }

        public T GetPanel<T>() where T : BaseInspectorContentPanel
        {
            return panels.OfType<T>().FirstOrDefault();
        }

        public void HidePanel()
        {
            activePanel?.Hide();
            activePanel = null;
        }

        public void OpenAssetLibrary()
        {
            ShowPanel<AssetLibraryPanel>();
        }

        public void OpenPolgyonGridPanel()
        {
            ShowPanel<InspectorPolygonGridPanel>();
        }

        public void ToggleImportAssetPanel()
        {
            if (inspectorPanel.IsOpen())
            {
                HidePanel();
                //do not use Close here to avoid the toggle notification
                InspectorPanel.Close(); 
            }
            else
            {   
                Open();
                InspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(true);
                ShowPanel<ImportAssetPanel>();
            }
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
