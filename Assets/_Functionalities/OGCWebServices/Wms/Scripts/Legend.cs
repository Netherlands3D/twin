using Netherlands3D.Twin.Layers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;
using Netherlands3D.Functionalities.OgcWebServices.Shared;
using Netherlands3D.Twin.UI;

namespace Netherlands3D.Functionalities.Wms
{
    public class LegendUrlContainer
    {
        // public string getCapabilitiesUrl;
        public Dictionary<string, string> LayerNameLegendUrlDictionary;

        public LegendUrlContainer(Dictionary<string, string> legendDictionary)
        {
            LayerNameLegendUrlDictionary = legendDictionary;
        }
    }
    
    public class Legend : MonoBehaviour
    {
        public static Dictionary<string, LegendUrlContainer> LegendUrlDictionary = new(); //key: getCapabilities url, Value: legend urls for that GetCapabilities
        private List<string> pendingRequests = new();

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

        [SerializeField] private GameObject mainPanel;
        [SerializeField] private RectTransform inactive;
        [SerializeField] private LegendImage graphicPrefab;

        // public LayerData CurrentLayer { get; set; }

        // private List<LegendImage> graphics = new List<LegendImage>();

        public void AddGraphic(Sprite sprite)
        {
            LegendImage image = Instantiate(graphicPrefab, graphicPrefab.transform.parent);
            image.gameObject.SetActive(true);
            image.SetSprite(sprite);
            // graphics.Add(image);

            GetComponentInChildren<LegendClampHeight>()?.AdjustRectHeight();
            GetComponent<ContentFitterRefresh>()?.RefreshContentFitters();
        }

        // public void ClearGraphics()
        // {
        //     if (graphics.Count == 0)
        //         return;
        //
        //     for (int i = graphics.Count - 1; i >= 0; i--)
        //     {
        //         Destroy(graphics[i].gameObject);
        //     }
        //
        //     graphics.Clear();
        // }

        public void GetLegendUrl(string layerUrl, Action<LegendUrlContainer> callback)
        {
            // var legendUri = new UriBuilder(propertyData.Data.GetLeftPart(UriPartial.Path));
            // legendUri.SetQueryParameter("service", "wms");
            // legendUri.SetQueryParameter("request", "getcapabilities");
            var getCapabilitiesURL = OgcWebServicesUtility.CreateGetCapabilitiesURL(layerUrl, ServiceType.Wms);
            StartCoroutine(RequestLegendUrls(getCapabilitiesURL, callback));

            //     legendUrls =>
            // {
            //     StartCoroutine(GetLegendGraphics(legendUrls));
            // }));


            if (LegendUrlDictionary.ContainsKey(getCapabilitiesURL))
            {
                callback.Invoke(LegendUrlDictionary[getCapabilitiesURL]);
                return;
            }

            if (pendingRequests.Contains(getCapabilitiesURL))
            {
                StartCoroutine(WaitForExistingRequestToComplete(getCapabilitiesURL, callback));
                return;
            }

            if (!OgcWebServicesUtility.IsValidUrl(new Uri(getCapabilitiesURL), RequestType.GetCapabilities))
            {
                Debug.LogError("Bounding boxes not in dictionary, and invalid getCapabilities url provided");
                callback.Invoke(null);
                return;
            }

            StartCoroutine(RequestLegendUrls(getCapabilitiesURL, callback));
        }

        private IEnumerator WaitForExistingRequestToComplete(string url, Action<LegendUrlContainer> callback)
        {
            while (pendingRequests.Contains(url))
            {
                yield return null;
            }

            callback.Invoke(LegendUrlDictionary[url]);
        }

        private IEnumerator RequestLegendUrls(string getCapabilitiesURL, Action<LegendUrlContainer> onLegendUrlsReceived)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(getCapabilitiesURL);
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download legends at {getCapabilitiesURL}");
            }
            else
            {
                var getCapabilities = new WmsGetCapabilities(new Uri(getCapabilitiesURL), webRequest.downloadHandler.text);
                var legendUrls = new LegendUrlContainer(getCapabilities.GetLegendUrls());
                LegendUrlDictionary[getCapabilitiesURL] = legendUrls;
                onLegendUrlsReceived.Invoke(legendUrls);
            }
        }

        public void HideLegend()
        {
            mainPanel.SetActive(false);
        }
        
        public void ShowLegend(string url)
        {
            mainPanel.SetActive(true);
            StartCoroutine(GetLegendGraphics(url));
        }
        
        private IEnumerator GetLegendGraphics(string url)
        {
            // ShowInactive(urls.Count == 0);
            // ClearGraphics();
            // if (urls.Count == 0)
            // {
            //     yield break;
            // }

            // foreach (string url in urls)
            // {
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
                else
                {
                    ShowInactive(true); // no legend available at specified url
                }
            // }
        }

        public void ShowInactive(bool show)
        {
            inactive.gameObject.SetActive(show);
        }
    }
}