using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D._Application._Twin;
using Netherlands3D.Catalogs;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class AssetLibraryPanel : BaseInspectorContentPanel
    {
        public override ToolbarInspector.ToolbarStyle ToolbarStyle => ToolbarInspector.ToolbarStyle.Library;
        
        private AssetLibrary assetLibrary;
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();
        private Breadcrumb breadcrumb;
        private Breadcrumb Breadcrumb => breadcrumb ??= this.Q<Breadcrumb>();

        public Action<ICatalogItem> OnOpenCatalogItem;
        
        public AssetLibraryPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += () => EnableInClassList("active", true);
            OnHide += () => EnableInClassList("active", false);

            // Virtualization and selection
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;

            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;
            Breadcrumb.CrumbClicked += OnBreadcrumbClicked;
        }

        public override string GetTitle() => "Toevoegen";

        public async void LoadCatalog(ICatalog catalog)
        {
            Breadcrumb.ClearCrumbs();

            var catalogItemCollection = await Load(async () => await catalog.BrowseAsync());
            await OpenFolder("Bibliotheek", catalogItemCollection);
        }

        private async Task<T> Load<T>(Func<Task<T>> callback)
        {
            // TODO: show loader
            var result = await callback();
            // TODO: Close loader

            return result;
        }

        private async Task OpenAsset(ICatalogItem catalogItem)
        {
            switch (catalogItem)
            {
                case ICatalog catalog: 
                    var catalogItemCollection = await Load(async () => await catalog.BrowseAsync());
                    await OpenFolder(catalog.Title, catalogItemCollection);
                    return;
                case ICatalogItemCollection collection: await OpenFolder(catalogItem.Title, collection); return;
                default: OnOpenCatalogItem?.Invoke(catalogItem); break;
            }
        }

        private async Task OpenFolder(string title, ICatalogItemCollection catalogItemCollection)
        {
            Breadcrumb.AddCrumb(title, catalogItemCollection);
            await LoadItemsIntoListView(catalogItemCollection);
        }

        private async void OnBreadcrumbClicked(int _, Breadcrumb.Crumb crumb)
        {
            await LoadItemsIntoListView(crumb.Target as ICatalogItemCollection);
        }

        private async Task LoadItemsIntoListView(ICatalogItemCollection catalogItemCollection)
        {
            var currentCatalogItems = await Load(catalogItemCollection.GetItemsAsync);

            ListView.itemsSource = currentCatalogItems.ToList();
            ListView.RefreshItems();
        }

        private VisualElement MakeListViewItem()
        {
            var button = new Button { name = "LayerButton" };
            var listViewItem = new ListViewItem(button);
            button.RegisterCallback<ClickEvent>(_ => OpenAsset(button.userData as ICatalogItem));
            
            return listViewItem;
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<Button>() is not Button button) return;
            
            ICatalogItem catalogItem = ListView.itemsSource[index] as ICatalogItem;
            button.LabelText = catalogItem.Title;
            var icon = catalogItem switch
            {
                ICatalogItemCollection => IconImage.Folder,
                ICatalog => IconImage.Library,
                _ => IconImage.Map
            };
            button.Image = icon;
            button.userData = catalogItem;
        }
    }
}
