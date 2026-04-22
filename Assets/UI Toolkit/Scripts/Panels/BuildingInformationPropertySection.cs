using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.GeoJSON;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Rendering;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(BuildingPropertyData))]
    public partial class BuildingInformationPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private const string idReplacementString = "{BagID}";
        private const string bagRequestUrl = "https://service.pdok.nl/lv/bag/wfs/v2_0?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:pand&count=100&outputFormat=xml&srsName=EPSG:28992&filter=%3cFilter%3e%3cPropertyIsEqualTo%3e%3cPropertyName%3eidentificatie%3c/PropertyName%3e%3cLiteral%3e{BagID}%3c/Literal%3e%3c/PropertyIsEqualTo%3e%3c/Filter%3e";
        private const string addressRequestUrl = "https://service.pdok.nl/lv/bag/wfs/v2_0?service=wfs&request=getFeature&version=2.0.0&outputFormat=geojson&typeName=bag:verblijfsobject&filter=%3CFilter%3E%3CPropertyIsEqualTo%3E%3CPropertyName%3Epandidentificatie%3C/PropertyName%3E%3CLiteral%3E{BagID}%3C/Literal%3E%3C/PropertyIsEqualTo%3E%3C/Filter%3E";
        private const string removeFromID = "NL.IMBAG.Pand.";
        
        private BuildingPropertyData buildingPropertyData;
        private Coroutine downloadProcess;
        private VisualElement thumbnailContainer;
        private Hyperlink bagLink;
        private Label statusValue;
        private Label yearValue;
        
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
            });
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
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
            
            LoadBagId(buildingIds.FirstOrDefault());
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
        
        private void LoadBagId(KeyValuePair<string, Coordinate> bagId)
        {
            string key = bagId.Key;
            if (removeFromID.Length > 0) key = key.Replace(removeFromID, "");

            ThumbnailService thumbnailService = ServiceLocator.GetService<ThumbnailService>();
            if (downloadProcess != null)
            {
                thumbnailService.StopCoroutine(downloadProcess);
            }
            downloadProcess = thumbnailService.StartCoroutine(GetBagIDData(key, bagId.Value));
        }

        private IEnumerator GetBagIDData(string bagID, Coordinate coordinate)
        {
            yield return GetBAGData(bagID, coordinate);
            yield return GetAddresses(bagID);
        }

        private IEnumerator GetBAGData(string bagID, Coordinate coordinate)
        {
            var requestUrl = bagRequestUrl.Replace(idReplacementString, bagID);
            var webRequest = UnityWebRequest.Get(requestUrl);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Clear();
                yield break;
            }
            
            string bagIdText;
            string yearText;
            string statusText;

            GeoJSONStreamReader customJsonHandler = new GeoJSONStreamReader(webRequest.downloadHandler.text);
            while (customJsonHandler.GotoNextFeature())
            {
                var properties = customJsonHandler.GetProperties();

                bagIdText = properties["identificatie"].ToString();
                yearText = properties["bouwjaar"].ToString();
                statusText = properties["status"].ToString();
                
                bagLink.text = bagID;
                bagLink.url = "https://bagviewer.kadaster.nl/lvbag/bag-viewer/?objectId=" + bagIdText;
                
                statusValue.text = statusText;
                yearValue.text = yearText;

                ThumbnailService thumbnailService = ServiceLocator.GetService<ThumbnailService>();
                
                //TODO: Use bbox and geometry.coordinates from GeoJSON object to create bounds to render thumbnail
                Bounds currentObjectBounds = new Bounds(coordinate.ToUnity(), Vector3.one * 50.0f);
                Texture2D tex = thumbnailService.RenderThumbnail(currentObjectBounds);
                thumbnailContainer.style.backgroundImage = new StyleBackground(tex);
                float aspect = (float)tex.height / tex.width;
                float newHeight = thumbnailContainer.resolvedStyle.width * aspect;
                thumbnailContainer.style.height = newHeight;
            }
        }
        
        private IEnumerator GetAddresses(string bagID)
        {
            var requestUrl = addressRequestUrl.Replace(idReplacementString, bagID);
            var webRequest = UnityWebRequest.Get(requestUrl);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Clear();
                yield break;
            }

            List<string> addresses = new List<string>();
            string districtText;
            
            GeoJSONStreamReader customJsonHandler = new GeoJSONStreamReader(webRequest.downloadHandler.text);
            bool gotDistrict = false;
            while (customJsonHandler.GotoNextFeature())
            {
                var properties = customJsonHandler.GetProperties();

                //Use first address result to determine district
                if (!gotDistrict)
                {
                    districtText = properties["openbare_ruimte"].ToString();
                    gotDistrict = true;
                }

                string address = $"{properties["openbare_ruimte"]} {properties["huisnummer"]} {properties["huisletter"]}{properties["toevoeging"]}";
                addresses.Add(address);
            }
            
            PopulateAddresses(addresses);
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