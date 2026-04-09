using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Events;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument))]
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

        private readonly HashSet<BaseInspectorContentPanel> panels = new();
        private BaseInspectorContentPanel activePanel;
        
        [SerializeField] 
        [Obsolete("Replaced by the OnUriImportStarted event as soon as copy/paste and credential support is added")]
        private UnityEvent OpenLegacyFileImportContentPanel;
        
        private ToolbarMain toolbarMain;
        private ToolbarMain ToolbarMain => toolbarMain ??= Root?.Q<ToolbarMain>();
        

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
            InspectorPanel.InspectorHeaderCloseButton.clicked += HidePanel;
            
            AssetLibraryPanel.OnShow += OnShowAssetLibrary;
            AssetLibraryPanel.OnHide += OnHideAssetLibrary;
            AssetLibraryPanel.OnOpenCatalogItem += OnOpenCatalogItem;

            GetPanel<ImportAssetPanel>().OnShow += OnShowImportAssetPanel;
            GetPanel<ImportAssetPanel>().OnHide += OnHideImportAssetPanel;
            
            ToolbarMain.AddButton.clicked += ShowPanel<ImportAssetPanel>;
        }

        private void OnDisable()
        {
            InspectorPanel.Toolbar.OnAddLayerToggled -= OnAddLayerToggled;
            InspectorPanel.Toolbar.OnOpenLibraryToggled -= OnOpenLibraryToggled;
            InspectorPanel.InspectorHeaderCloseButton.clicked -= HidePanel;

            AssetLibraryPanel.OnShow -= OnShowAssetLibrary;
            AssetLibraryPanel.OnHide -= OnHideAssetLibrary;
            AssetLibraryPanel.OnOpenCatalogItem -= OnOpenCatalogItem;

            GetPanel<ImportAssetPanel>().OnShow -= OnShowImportAssetPanel;
            GetPanel<ImportAssetPanel>().OnHide -= OnHideImportAssetPanel;
            
            ToolbarMain.AddButton.clicked -= ShowPanel<ImportAssetPanel>;
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
        public BaseInspectorContentPanel RegisterPanel<T>() where T : BaseInspectorContentPanel, new()
        {
            var panel = new T();
            panels.Add(panel);

            InspectorPanel.Content.Add(panel);
            panel.Hide();

            return panel;
        }

        public void ShowPanel<T>() where T : BaseInspectorContentPanel
        {
            BaseInspectorContentPanel previousPanel = activePanel;
            // only one panel can be open at a time
            HidePanel();

            //was the activepanel already open? then toggle it to close
            if (previousPanel == GetPanel<T>())
            {
                return;
            }
            
            Open();
            activePanel = GetPanel<T>();
            InspectorPanel.HeaderText = activePanel.GetTitle();
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

            // TODO: At the moment - the InspectorPanel is only available for the Asset Library; once we add more
            // onto this panel, remove this line as it shouldn't auto-close yet
            Close();
        }

        private void OnAddLayerToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue) ShowPanel<ImportAssetPanel>();
            else HidePanel();
        }

        private void OnOpenLibraryToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue) ShowPanel<AssetLibraryPanel>();
            else HidePanel();
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
