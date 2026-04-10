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
    public partial class FeatureInformationPropertySection : VisualElement, IVisualizationWithPropertyData
    { 
        private FeaturePropertyData featurePropertyData;
        private Coroutine downloadProcess;
        private VisualElement thumbnailContainer;
        
        private ListView addressListView;
        private ListView AddressListView => addressListView ??= this.Q<ListView>();

        public BuildingInformationPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            thumbnailContainer = this.Q<VisualElement>("ThumbnailContainer");
            
            AddressListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            AddressListView.selectionType = SelectionType.None;
            
            AddressListView.makeItem = MakeListViewItem;
            AddressListView.bindItem = BindListViewItem;
            
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                featurePropertyData.OnIdsChanged.RemoveListener(OnIdsChanged);
            });
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            featurePropertyData = properties.Get<BuildingPropertyData>();
            featurePropertyData.OnIdsChanged.AddListener(OnIdsChanged);
            
            Dictionary<string, Coordinate> buildingIds = featurePropertyData.BuildingIds;
            if (buildingIds == null || buildingIds.Count == 0)
            {
                Clear();
                return;
            }

            LoadBagId(buildingIds.FirstOrDefault());
            thumbnailContainer.schedule.Execute(() => {  });
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

            if (downloadProcess != null)
            {
                ThumbnailCoroutineRunner.Instance.StopCoroutine(downloadProcess);
            }

            downloadProcess = ThumbnailCoroutineRunner.Instance.StartCoroutine(GetBagIDData(key, bagId.Value));
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

                //TODO: Use bbox and geometry.coordinates from GeoJSON object to create bounds to render thumbnail
                Bounds currentObjectBounds = new Bounds(coordinate.ToUnity(), Vector3.one * 50.0f);
                RenderTexture rTex = RenderedThumbnail.RenderThumbnail(currentObjectBounds);
                Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGBA32, false);

                RenderTexture.active = rTex;
                tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                thumbnailContainer.style.backgroundImage = new StyleBackground(tex);
                float aspect = (float)rTex.height / rTex.width;
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