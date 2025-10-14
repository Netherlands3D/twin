using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Catalogs;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI.Manipulators;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class AssetLibraryPanel : BaseInspectorContentPanel
    {
        public override ToolbarInspector.ToolbarStyle ToolbarStyle => ToolbarInspector.ToolbarStyle.Library;
        
        private AssetLibrary.AssetLibrary assetLibrary;
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

            // TODO: Until we officially support pagination - set the page limit to the max of 1000
            var pagination = new Pagination(0, 1000);
            
            var catalogItemCollection = await Load(async () => await catalog.BrowseAsync(pagination));
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
            var listViewItem = CreateListViewItemVisualElement();
            listViewItem.AddManipulator(
                new DragToWorldManipulator(
                    this.panel.visualTree, 
                    CreateGhost, 
                    (target, _) => OpenAsset(target.userData as ICatalogItem)
                )
            );
            
            return listViewItem;
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            
            ICatalogItem catalogItem = ListView.itemsSource[index] as ICatalogItem;
            listViewItem.userData = catalogItem;
            
            UpdateListViewItem(listViewItem);
        }

        private static ListViewItem CreateListViewItemVisualElement()
        {
            return new ListViewItem(new Button { name = "LayerButton" });
        }

        private static ListViewItem CreateGhost(VisualElement target)
        {
            var visualElement = CreateListViewItemVisualElement();
            visualElement.userData = target.userData;
            UpdateListViewItem(visualElement);
            return visualElement;
        }

        private static void UpdateListViewItem(ListViewItem listViewItem)
        {
            if (listViewItem.Q<Button>() is not Button button) return;
            if (listViewItem.userData is not ICatalogItem catalogItem) return;
            
            button.LabelText = catalogItem.Title;
            button.Image = catalogItem is ICatalogItemCollection ? IconImage.Folder : IconImage.Map;
        }
    }
}
