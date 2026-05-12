using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Credentials;
using Netherlands3D.Events;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
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
    
        private VisualElement root => appDocument?.rootVisualElement;
        private InspectorPanel inspectorPanel => root?.Q<InspectorPanel>();
        private AssetLibraryPanel assetLibraryPanel => panels.OfType<AssetLibraryPanel>().FirstOrDefault();
        private ImportAssetPanel importAssetPanel => panels.OfType<ImportAssetPanel>().FirstOrDefault();
        private InspectorPolygonGridPanel polygonGridPanel => panels.OfType<InspectorPolygonGridPanel>().FirstOrDefault();
        private InspectorDownloadGridPanel downloadGridPanel => panels.OfType<InspectorDownloadGridPanel>().FirstOrDefault();

        private readonly HashSet<BaseInspectorContentPanel> panels = new();
        private BaseInspectorContentPanel activePanel;
        private ToolbarMain toolbarMain => root?.Q<ToolbarMain>();
        
        private ICredentialHandler credentialHandler;

        [SerializeField] private TriggerEvent OnDrawNewGrid;
        [SerializeField] private TriggerEvent OnGridConfirmed;

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            credentialHandler = GetComponent<ICredentialHandler>();
            RegisterPanel<AssetLibraryPanel>(assetLibrary);
            RegisterPanel<ImportAssetPanel>();
            RegisterPanel<InspectorPolygonGridPanel>();
            
            inspectorPanel.Close();
            
            importAssetPanel.SetCredentialHandler(credentialHandler);
        }

        private void OnEnable()
        {
            inspectorPanel.Toolbar.OnAddLayerToggled += OnAddLayerToggled;
            inspectorPanel.Toolbar.OnOpenLibraryToggled += OnOpenLibraryToggled;
            inspectorPanel.InspectorHeaderCloseButton.clicked += Close;
            importAssetPanel.OpenAssetLibrary += OpenAssetLibrary;
            importAssetPanel.importSucceeded.AddListener(OnImportSucceeded);
            
            toolbarMain.AddButton.clicked += ToggleImportAssetPanel;
            
            OnDrawNewGrid.AddListenerStarted(OpenPolgyonGridPanel);
            
            polygonGridPanel.OnConfirmSelection.AddListener(OnGridConfirmed.InvokeStarted);
            //TODO ongridconfirmed -> open layerpanel and close the gridpanel (if its not automatically happening)

        }

        private void OnDisable()
        {
            inspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            inspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            inspectorPanel.InspectorHeaderCloseButton.clicked -= Close;
            importAssetPanel.OpenAssetLibrary -= OpenAssetLibrary;
            importAssetPanel.importSucceeded.RemoveListener(OnImportSucceeded);
            
            toolbarMain.AddButton.clicked -= ToggleImportAssetPanel;
            
            OnDrawNewGrid.RemoveListenerStarted(OpenPolgyonGridPanel);
            
            polygonGridPanel.OnConfirmSelection.RemoveListener(OnGridConfirmed.InvokeStarted);
        }

        public void Open()
        {
            inspectorPanel.Open();
        }

        public void Close()
        {
            toolbarMain.ClearWithoutNotify();
            inspectorPanel.Toolbar.ToggleButtonsOffWithoutNotify();
            HidePanel();
            inspectorPanel.Close();
        }

        // TODO: Shouldn't this be in the InspectorPanel component?
        public BaseInspectorContentPanel RegisterPanel<T>(params object[] args) where T : BaseInspectorContentPanel
        {
            var panel = (T)Activator.CreateInstance(typeof(T), args);
            panels.Add(panel);
            inspectorPanel.Content.Add(panel);
            panel.Hide();
            return panel;
        }

        public void ShowPanel<T>() where T : BaseInspectorContentPanel
        {
            // only one panel can be open at a time
            HidePanel();
            Open();
            activePanel = GetPanel<T>();
            inspectorPanel.HeaderText = activePanel.Title;
            inspectorPanel.ToolbarStyle = activePanel.ToolbarStyle;
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
                inspectorPanel.Close(); 
            }
            else
            {   
                Open();
                inspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(true);
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
            inspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(false);
            Close();
        }
    }
}
