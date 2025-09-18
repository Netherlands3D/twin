using System.Collections.Generic;
using System.Linq;
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
        private ListView listView;
        private List<ICatalogItem> currentPageWithItems;
        private AssetLibrary assetLibrary;
        private ListView ListView => listView ??= this.Q<ListView>();

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

            // TODO: show loader
            var itemCollectionPage = await assetLibrary.Catalog.BrowseAsync();
            var catalogItems = await itemCollectionPage.GetItemsAsync();
            
            currentPageWithItems = catalogItems.ToList();
            ListView.itemsSource = currentPageWithItems;
            ListView.bindItem = BindListViewItem;
            ListView.RefreshItems();
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            var content = item.Q("Content");
            if (content == null) return;

            var button = EnsureButtonExists(content);

            ICatalogItem catalogItem = currentPageWithItems[index];
            button.LabelText = catalogItem.Title;
            button.Image = Icon.IconImage.Folder;
            button.RegisterCallback<ClickEvent>(evt =>
            {
                assetLibrary.Load(catalogItem);
            });
        }

        private static Button EnsureButtonExists(VisualElement content)
        {
            Button button = content.Q<Button>("LayerButton");
            if (button != null) return button;
            
            button = new Button
            {
                name = "LayerButton",
                style =
                {
                    alignSelf = Align.Stretch,
                    flexGrow = 1
                }
            };
            content.Add(button);

            return button;
        }
    }
}
