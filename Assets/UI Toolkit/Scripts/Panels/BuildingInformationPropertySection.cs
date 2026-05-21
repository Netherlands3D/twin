using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(BuildingPropertyData), PropertySectionCategory.Information)]
    public partial class BuildingInformationPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private BuildingPropertyData buildingPropertyData;
        private VisualElement thumbnailContainer;
        private Hyperlink bagLink;
        private Label statusValue;
        private Label yearValue;
        private BagDataService.BagRequestHandle handle;
        private ListView addressListView;
        private ListView AddressListView => addressListView ??= this.Q<ListView>();

        public BuildingInformationPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            thumbnailContainer = this.Q<VisualElement>("ThumbnailContainer");
            bagLink = this.Q<Hyperlink>("Link");
            statusValue = this.Q<Label>("StatusValue");
            yearValue = this.Q<Label>("YearValue");
            
            AddressListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            AddressListView.selectionType = SelectionType.None;
            
            AddressListView.makeItem = MakeListViewItem;
            AddressListView.bindItem = BindListViewItem;
            
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                buildingPropertyData.OnIdsChanged.RemoveListener(OnIdsChanged);
                handle = null;
            });
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            handle = new BagDataService.BagRequestHandle
            {
                OnFailed = Clear,
                OnAddresses = PopulateAddresses,
                OnBagData = UpdateThumbnail
            };
            buildingPropertyData = properties.Get<BuildingPropertyData>();
            buildingPropertyData.OnIdsChanged.AddListener(OnIdsChanged);
            
            Dictionary<string, Coordinate> buildingIds = buildingPropertyData.BuildingIds;
            OnIdsChanged(buildingIds);
        }

        private void OnIdsChanged(Dictionary<string, Coordinate> buildingIds)
        {
            if (buildingIds == null || buildingIds.Count == 0)
            {
                Clear();
                return;
            }
            
            BagDataService bagDataService = ServiceLocator.GetService<BagDataService>();
            bagDataService.LoadBagId(buildingIds.FirstOrDefault().Key, handle);
        }
        
        public void PopulateAddresses(List<string> addresses)
        {
            AddressListView.itemsSource = addresses;
            AddressListView.RefreshItems();
        }
        
        private VisualElement MakeListViewItem()
        {
            return new Label();
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not Label listViewItem) return;
            
             string text = AddressListView.itemsSource[index] as string;
             listViewItem.text = text;
        }
        
        private void UpdateThumbnail(BagDataService.BagData bagData)
        {
            Dictionary<string, Coordinate> buildingIds = buildingPropertyData.BuildingIds;
            Coordinate coordinate = buildingIds[bagData.id];
            bagLink.text = bagData.id;
            bagLink.url = bagData.url;
            statusValue.text = bagData.status;
            yearValue.text = bagData.year;
            
            ThumbnailService thumbnailService = ServiceLocator.GetService<ThumbnailService>();
            //TODO: Use bbox and geometry.coordinates from GeoJSON object to create bounds to render thumbnail
            Bounds currentObjectBounds = new Bounds(coordinate.ToUnity(), Vector3.one * 50.0f);
            Texture2D tex = thumbnailService.RenderThumbnail(currentObjectBounds);
            thumbnailContainer.style.backgroundImage = new StyleBackground(tex);
            float aspect = (float)tex.height / tex.width;
            float newHeight = thumbnailContainer.resolvedStyle.width * aspect;
            thumbnailContainer.style.height = newHeight;
        }

        private void Clear()
        {
            thumbnailContainer.style.height = 0;
            PopulateAddresses(new List<string>() { "Geen adressen gevonden" });
            bagLink.text = "";
            bagLink.url = "";
                
            statusValue.text = "";
            yearValue.text = "";
        }
    }
}