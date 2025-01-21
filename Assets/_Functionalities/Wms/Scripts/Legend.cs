using Netherlands3D.Twin.Layers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Web;
using UnityEngine.Networking;
using System.Xml;
using Netherlands3D.Twin.UI;

namespace Netherlands3D.Functionalities.Wms
{
    public class Legend : MonoBehaviour
    {
        public static Legend Instance
        {
            get
            {
                if (instance == null)
                {   
                    instance = FindObjectOfType<Legend>(true);
                }
                return instance;
            }
        }

        private static Legend instance;

        [SerializeField] private RectTransform inactive;
        [SerializeField] private LegendImage graphicPrefab;

        public LayerData CurrentLayer { get; set; }

        private List<LegendImage> graphics = new List<LegendImage>();


        public void AddGraphic(Sprite sprite)
        {
            LegendImage image = Instantiate(graphicPrefab, graphicPrefab.transform.parent);
            image.gameObject.SetActive(true);
            image.SetSprite(sprite);
            graphics.Add(image);

            GetComponentInChildren<LegendClampHeight>()?.AdjustRectHeight();
            GetComponent<ContentFitterRefresh>()?.RefreshContentFitters();
        }

        public void ClearGraphics()
        {
            if (graphics.Count == 0)
                return;

            for(int i = graphics.Count - 1; i >= 0; i--)
            {
                Destroy(graphics[i].gameObject);
            }
            graphics.Clear();
        }

        public void LoadLegend(LayerGameObject obj)
        {
            if (CurrentLayer == obj.LayerData.ParentLayer)
                return;

            CurrentLayer = obj.LayerData.ParentLayer;

            ILayerWithPropertyData layer = obj as ILayerWithPropertyData;
            LayerURLPropertyData propertyData = layer.PropertyData as LayerURLPropertyData;
            var legendUri = new UriBuilder(propertyData.Data.GetLeftPart(UriPartial.Path));
            legendUri.SetQueryParameter("service", "wms");
            legendUri.SetQueryParameter("request", "getcapabilities");
            StartCoroutine(GetCapabilities(legendUri.Uri.ToString(), legendUrls =>
            {
                StartCoroutine(GetLegendGraphics(legendUrls));
            }));
        }

        private IEnumerator GetCapabilities(string url, Action<List<string>> callBack)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download {url}");
            }
            else
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(webRequest.downloadHandler.text);
                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(doc.NameTable);
                namespaceManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink"); // Update this URI if necessary

                List<string> legendUrls = new List<string>();
                XmlNodeList layers = doc.GetElementsByTagName("Layer");
                foreach (XmlNode layer in layers)
                {
                    XmlNodeList legendNodes = layer.SelectNodes(".//*[local-name()='LegendURL']/*[local-name()='OnlineResource']", namespaceManager);
                    foreach (XmlNode legendNode in legendNodes)
                    {
                        string legendUrl = legendNode.Attributes["xlink:href"]?.Value;
                        if (!string.IsNullOrEmpty(legendUrl) && !legendUrls.Contains(legendUrl))
                        {
                            legendUrls.Add(legendUrl);
                        }
                    }
                }
                callBack.Invoke(legendUrls);
            }
        }

        private IEnumerator GetLegendGraphics(List<string> urls)
        {
            ShowInactive(urls.Count == 0);
            ClearGraphics();
            if (urls.Count == 0)
            {
                yield break;
            }

            foreach (string url in urls)
            {
                UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Texture texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                    Texture2D tex = texture as Texture2D;
                    tex.Apply(false, true);
                    Sprite imageSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), Vector2.one * 0.5f, 100);
                    AddGraphic(imageSprite);
                }
            }
        }

        public void ShowInactive(bool show)
        {
            inactive.gameObject.SetActive(show);
        }

    }
}
