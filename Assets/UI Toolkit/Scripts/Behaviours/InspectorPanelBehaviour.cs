using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Events;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument))]
    public class InspectorPanelBehaviour : MonoBehaviour
    {
        private UIDocument appDocument;
        [SerializeField] private AssetLibrary assetLibrary;
        [SerializeField] private TriggerEvent uploadFileEvent;
    
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

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            RegisterPanel<AssetLibraryPanel>();
            RegisterPanel<ImportAssetPanel>();
        }

        private void OnEnable()
        {
            InspectorPanel.Toolbar.OnAddLayerToggled += OnAddLayerToggled;
            InspectorPanel.Toolbar.OnOpenLibraryToggled += OnOpenLibraryToggled;
            
            AssetLibraryPanel.OnShow += OnShowAssetLibrary;
            AssetLibraryPanel.OnHide += OnHideAssetLibrary;
            AssetLibraryPanel.OnOpenCatalogItem += OnOpenCatalogItem;

            ImportAssetPanel.OnShow += OnShowAssetLibrary;
            ImportAssetPanel.OnHide += OnHideAssetLibrary;
            ImportAssetPanel.FileUploadStarted += OnUploadStarted;
            ImportAssetPanel.UriImportStarted += OnUriImportStarted;
        }

        private void OnDisable()
        {
            InspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            AssetLibraryPanel.OnShow -= OnShowAssetLibrary;
            AssetLibraryPanel.OnHide -= OnHideAssetLibrary;
            AssetLibraryPanel.OnOpenCatalogItem -= OnOpenCatalogItem;

            ImportAssetPanel.OnShow -= OnShowImportAssetPanel;
            ImportAssetPanel.OnHide -= OnHideImportAssetPanel;
            ImportAssetPanel.FileUploadStarted -= OnUploadStarted;
            ImportAssetPanel.UriImportStarted -= OnUriImportStarted;
        }

        public void Open()
        {
            InspectorPanel.Open();
        }

        public void Close()
        {
            InspectorPanel.Close();
        }

        // TODO: Shouldn't this be in the InspectorPanel component?
        public BaseInspectorContentPanel RegisterPanel<T>() where T : BaseInspectorContentPanel,new()
        {
            return RegisterPanel(new T());
        }

        // TODO: Shouldn't this be in the InspectorPanel component?
        public BaseInspectorContentPanel RegisterPanel(BaseInspectorContentPanel panel)
        {
            panels.Add(panel);
            InspectorPanel.Content.Add(panel);
            
            // Ensure panel is hidden by default
            panel.Hide();
            
            return panel;
        }

        public void ShowPanel<T>() where T : BaseInspectorContentPanel
        {
            // only one panel can be open at a time
            HidePanel();
            
            Open();
            activePanel = panels.OfType<T>().FirstOrDefault();
            InspectorPanel.HeaderText = activePanel.GetTitle();
            InspectorPanel.ToolbarStyle = activePanel.ToolbarStyle;
            activePanel.Show();
        }

        public void HidePanel()
        {
            activePanel?.Hide();
        }

        public void OpenAssetLibrary() => ShowPanel<AssetLibraryPanel>();
        public void CloseAssetLibrary() => HidePanel();

        // TODO: Shouldn't this be in the InspectorPanel component?
        private void OnShowAssetLibrary()
        {
            AssetLibraryPanel.LoadCatalog(assetLibrary.Catalog);

            InspectorPanel.Toolbar.OpenLibrary.SetValueWithoutNotify(true);
        }

        // TODO: Shouldn't this be in the InspectorPanel component?
        private void OnHideAssetLibrary()
        {
            InspectorPanel.Toolbar.OpenLibrary.SetValueWithoutNotify(false);
            
            // TODO: At the moment - the InspectorPanel is only available for the Asset Library; once we add more
            // onto this panel, remove this line as it shouldn't auto-close yet
            Close();
        }

        public void OpenImportAssetPanel() => ShowPanel<ImportAssetPanel>();
        public void CloseImportAssetPanel() => HidePanel();

        // TODO: Shouldn't this be in the InspectorPanel component?
        private void OnShowImportAssetPanel()
        {
            InspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(true);
        }

        // TODO: Shouldn't this be in the InspectorPanel component?
        private void OnHideImportAssetPanel()
        {
            InspectorPanel.Toolbar.AddLayer.SetValueWithoutNotify(false);
        }
        

        private void OnAddLayerToggled(ChangeEvent<bool> evt)
        {
            if (!evt.newValue)
            {
                CloseImportAssetPanel();
                return;
            }

            OpenImportAssetPanel();
        }

        private void OnOpenLibraryToggled(ChangeEvent<bool> evt)
        {
            if (!evt.newValue)
            {
                CloseAssetLibrary();
                return;
            }

            OpenAssetLibrary();
        }

        private void OnUploadStarted(ClickEvent evt)
        {
            uploadFileEvent.InvokeStarted();
            
            Close();
        }

        private void OnUriImportStarted(Uri uri)
        {
            App.Layers.Add(LayerBuilder.Create().FromUrl(uri));
            
            Close();
        }

        private void OnOpenCatalogItem(ICatalogItem catalogItem)
        {
            switch (catalogItem)
            {
                case RecordItem recordItem: assetLibrary.Load(recordItem); return;
                case DataService dataService: assetLibrary.Trigger(dataService); return;
                default:
                    Debug.LogError(
                        $"Tried to open catalog item with type {catalogItem.GetType().Name}, but this is not a record item"
                    );
                    break;
            }
        }
    }
}
