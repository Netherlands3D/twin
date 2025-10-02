using System.Linq;
using Netherlands3D._Application._Twin;
using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class AssetLibraryPanel : BaseInspectorContentPanel
    {
        private AssetLibrary assetLibrary;
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();
        private Breadcrumb breadcrumb;
        private Breadcrumb Breadcrumb => breadcrumb ??= this.Q<Breadcrumb>();

        public AssetLibraryPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            // Virtualization and selection
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;

            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;
            Debug.Log("Created AssetLibraryPanel");
            Breadcrumb.CrumbClicked += OnBreadcrumbClicked;
        }

        public override string GetTitle() => "Toevoegen";
        
        private void OnBreadcrumbClicked(int _, Breadcrumb.Crumb crumb)
        {
            ShowItemsFromCollection(crumb.Target as ICatalogItemCollection);
        }

        public void Open()
        {
            EnableInClassList("active", true);
        }

        public void Close()
        {
            EnableInClassList("false", false);
        }

        public async void SetAssetLibrary(AssetLibrary assetLibrary)
        {
            this.assetLibrary = assetLibrary;
            Breadcrumb.ClearCrumbs();

            // TODO: show loader

            var catalogItemCollection = await assetLibrary.Catalog.BrowseAsync();
            Breadcrumb.AddCrumb("Bibliotheek", catalogItemCollection);
            ShowItemsFromCollection(catalogItemCollection);
        }

        private async void ShowItemsFromCollection(ICatalogItemCollection itemCollectionPage)
        {
            // TODO: show loader
            
            var currentCatalogItems = await itemCollectionPage.GetItemsAsync();

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
            button.Image = catalogItem is ICatalogItemCollection ? IconImage.Folder : IconImage.Map;
            button.userData = catalogItem;
        }

        private void OpenAsset(ICatalogItem catalogItem)
        {
            switch (catalogItem)
            {
                case ICatalogItemCollection collection:
                    Breadcrumb.AddCrumb(catalogItem.Title, collection);
                    ShowItemsFromCollection(collection);
                    return;
                case RecordItem recordItem: assetLibrary.Load(recordItem); break;
                case DataService dataServiceItem: assetLibrary.Trigger(dataServiceItem); break;
                default: Debug.LogWarning("Attempted to open an unknown type of catalog item"); break;
            }
        }
    }
}
