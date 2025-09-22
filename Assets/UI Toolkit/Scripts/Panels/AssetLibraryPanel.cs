using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D._Application._Twin;
using Netherlands3D.Catalogs;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class AssetLibraryPanel : VisualElement
    {
        private AssetLibrary assetLibrary;
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();
        private Breadcrumb breadcrumb;
        private Breadcrumb Breadcrumb => breadcrumb ??= this.Q<Breadcrumb>();

        public AssetLibraryPanel()
        {
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/Panels/" + nameof(AssetLibraryPanel));
            asset.CloneTree(this);

            // Find and load USS stylesheet specific for this component (using -style)
            var styleSheet = Resources.Load<StyleSheet>("UI/Panels/" + nameof(AssetLibraryPanel) + "-style");
            styleSheets.Add(styleSheet);
            
            // Virtualization and selection
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;
            
            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;
            Breadcrumb.CrumbClicked += OnBreadcrumbClicked;
        }

        private void OnBreadcrumbClicked(int _, Breadcrumb.Crumb crumb)
        {
            ShowItemsFromCollection(crumb.Target as ICatalogItemCollection);
        }

        public void Show()
        {
            style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            style.display = DisplayStyle.None;
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
            button.RegisterCallback<ClickEvent>(_ => OpenAsset(button.userData as ICatalogItem));
            
            var listViewItem = new ListViewItem();
            listViewItem.Add(button);

            return listViewItem;
        }

        private void OpenAsset(ICatalogItem catalogItem)
        {
            if (catalogItem is ICatalogItemCollection collection)
            {
                Breadcrumb.AddCrumb(catalogItem.Title, collection);
                ShowItemsFromCollection(collection);
                return;
            }

            assetLibrary.Load(catalogItem);
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<Button>() is not Button button) return;
            
            ICatalogItem catalogItem = ListView.itemsSource[index] as ICatalogItem;
            button.LabelText = catalogItem.Title;
            button.Image = catalogItem is ICatalogItemCollection ? Icon.IconImage.Folder : Icon.IconImage.Map;
            button.userData = catalogItem;
        }
    }
}
