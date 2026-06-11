using Netherlands3D.Catalogs;
using Netherlands3D.Catalogs.CatalogItems;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects.ExtensionMethods;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class AssetLibraryPanel : BaseInspectorContentPanel
    {
        public override string Title => "Toevoegen";
        
        private AssetLibrary.AssetLibrary assetLibrary;
        private ListView listView;
        private Breadcrumb breadcrumb;
        private Button importButton;
        
        public AssetLibraryPanel(){}
        public AssetLibraryPanel(AssetLibrary.AssetLibrary assetLibrary) : this()
        {
            this.assetLibrary = assetLibrary;
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            listView = this.Q<ListView>();
            breadcrumb = this.Q<Breadcrumb>();
            importButton = this.Q<Button>("ImportButton");
            importButton.RegisterCallback<ClickEvent>(ImportAssets);

            // Virtualization and selection
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.selectionType = SelectionType.Multiple;

            listView.makeItem = MakeListViewItem;
            listView.bindItem = BindListViewItem;
            breadcrumb.CrumbClicked += OnBreadcrumbClicked;

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                LoadCatalog(assetLibrary.Catalog);
            });
        }

        public async void LoadCatalog(ICatalog catalog)
        {
            breadcrumb.ClearCrumbs();
            listView.ClearSelection();

            // TODO: Until we officially support pagination - set the page limit to the max of 1000
            var pagination = new Pagination(0, 1000);
            
            var catalogItemCollection = await catalog.BrowseAsync(pagination);
            await OpenFolder("Bibliotheek", catalogItemCollection);
        }

        private async Task OpenFolder(string title, ICatalogItemCollection catalogItemCollection)
        {
            breadcrumb.AddCrumb(title, catalogItemCollection);
            listView.ClearSelection();
            await LoadItemsIntoListView(catalogItemCollection);
        }

        private async void OnBreadcrumbClicked(int _, Breadcrumb.Crumb crumb)
        {
            listView.ClearSelection();
            await LoadItemsIntoListView(crumb.Target as ICatalogItemCollection);
        }

        private async Task LoadItemsIntoListView(ICatalogItemCollection catalogItemCollection)
        {
            var currentCatalogItems = await catalogItemCollection.GetItemsAsync();

            listView.itemsSource = currentCatalogItems.ToList();
            listView.RefreshItems();
        }

        private VisualElement MakeListViewItem()
        { 
            var assetLibraryListViewItem = new AssetLibraryListViewItem();
            var listViewItem = new ListViewItem(assetLibraryListViewItem);
            listViewItem.RegisterCallback<ClickEvent>(async _ =>
            {
                if (listViewItem.userData is not ICatalogItem item)
                    return;

                switch (item)
                {
                    case ICatalog catalog:
                        {                            
                            await OpenFolder(catalog.Title, await catalog.BrowseAsync());
                            break;
                        }

                    case ICatalogItemCollection collection:
                        {
                            await OpenFolder(item.Title, collection);
                            break;
                        }
                }
            });
            return listViewItem;
        }

        private void ImportAssets(ClickEvent evt)
        {
            var selectedItems = listView.selectedItems
                .Cast<ICatalogItem>()
                .ToList();

            foreach (var item in selectedItems)
            {
                switch (item)
                {
                    case RecordItem recordItem: assetLibrary.Load(recordItem); 
                        break;
                    case DataService dataService: assetLibrary.Trigger(dataService);
                        break;
                    default:
                        Debug.LogError(
                            $"Tried to open catalog item with type {item.GetType().Name}, but this is not a record item"
                        );
                        break;
                }
            }

            listView.ClearSelection();
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;

            AssetLibraryListViewItem assetItem = listViewItem.Q<AssetLibraryListViewItem>();

            ICatalogItem catalogItem = listView.itemsSource[index] as ICatalogItem;
            assetItem.LabelText = catalogItem.Title;
            var icon = catalogItem switch
            {
                ICatalogItemCollection => IconImage.Folder,
                ICatalog => IconImage.Library,
                _ => IconImage.Map
            };

            RecordItem recordItem = catalogItem as RecordItem;
            if(recordItem != null)
            {
                if (recordItem.Url.IsRemoteAsset())
                    icon = IconImage.Link;
                else
                {
                    string prefabId = recordItem.Url.AbsolutePath.Trim('/');
                    icon = LayerTypeSpriteLibrary.GetIconImage(prefabId);
                }
            }         

            assetItem.Image = icon;
            listViewItem.userData = catalogItem;
        }
    }
}
