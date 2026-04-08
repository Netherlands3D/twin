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
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

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

        public BuildingInformationPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            thumbnailContainer = this.Q<VisualElement>("ThumbnailContainer");
            
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
            if(buildingIds.Count == 0) return;

            thumbnailContainer.schedule.Execute(() => { LoadBagId(buildingIds.FirstOrDefault()); });
        }

        private void OnIdsChanged(Dictionary<string, Coordinate> buildingIds)
        {
            if(buildingIds.Count == 0) return;
            
            LoadBagId(buildingIds.FirstOrDefault());
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
         
            //Adressess (slower request next)
            //yield return GetAddresses(bagID);
        }

        private IEnumerator GetBAGData(string bagID, Coordinate coordinate)
        {
            var requestUrl = bagRequestUrl.Replace(idReplacementString, bagID);
            var webRequest = UnityWebRequest.Get(requestUrl);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Geen BAG data gevonden");
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
    }
}