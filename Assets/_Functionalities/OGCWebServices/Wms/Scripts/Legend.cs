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
        public string GetCapabilitiesUrl;
        public Dictionary<string, string> LayerNameLegendUrlDictionary;

        public LegendUrlContainer(string getCapabilitiesUrl, Dictionary<string, string> legendDictionary)
        {
            GetCapabilitiesUrl = getCapabilitiesUrl;
            LayerNameLegendUrlDictionary = legendDictionary;
        }
    }
    
    public class Legend : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private RectTransform inactive;
        [SerializeField] private LegendImage graphicPrefab;

        private LegendClampHeight legendClampHeight;
        private ContentFitterRefresh mainPanelContentFitterRefresh;
            
        public static Dictionary<string, LegendUrlContainer> LegendUrlDictionary = new(); //key: getCapabilities url, Value: legend urls for that GetCapabilities
        private List<string> pendingRequests = new();
        private string activeLegendUrl;
        private Coroutine runningCoroutine;
        private List<LegendImage> graphics = new List<LegendImage>();

        private static Legend instance;
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

        private void Awake()
        {
            legendClampHeight = GetComponentInChildren<LegendClampHeight>(true);
            mainPanelContentFitterRefresh = mainPanel.GetComponent<ContentFitterRefresh>();
        }

        public void AddGraphic(Sprite sprite)
        {
            LegendImage image = Instantiate(graphicPrefab, graphicPrefab.transform.parent);
            image.gameObject.SetActive(true);
            image.SetSprite(sprite);
            graphics.Add(image);

            legendClampHeight.AdjustRectHeight();
            mainPanelContentFitterRefresh.RefreshContentFitters();
        }

        private void ClearGraphics()
        {
            if (graphics.Count == 0)
                return;
        
            for (int i = graphics.Count - 1; i >= 0; i--)
            {
                Destroy(graphics[i].gameObject);
            }
        
            graphics.Clear();
        }

        public void GetLegendUrl(string layerUrl, Action<LegendUrlContainer> callback)
        {
            var getCapabilitiesURL = OgcWebServicesUtility.CreateGetCapabilitiesURL(layerUrl, ServiceType.Wms);

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
                var legendUrls = new LegendUrlContainer(getCapabilitiesURL, getCapabilities.GetLegendUrls());
                LegendUrlDictionary[getCapabilitiesURL] = legendUrls;
                onLegendUrlsReceived.Invoke(legendUrls);
            }
        }

        public void ShowLegend(string wmsUrl, bool show)
        {
            var getCapabilitiesUrl = OgcWebServicesUtility.CreateGetCapabilitiesURL(wmsUrl, ServiceType.Wms);
            mainPanel.SetActive(show);
            if (activeLegendUrl == getCapabilitiesUrl)
            {
                if (!show)
                    activeLegendUrl = null;
                return; //legend that should be set active is already loaded, so no further action is needed.
            }

            if (runningCoroutine != null)
                StopCoroutine(runningCoroutine);
            
            ClearGraphics();
            var urlContainer = LegendUrlDictionary[getCapabilitiesUrl];
            activeLegendUrl = getCapabilitiesUrl;
            
            runningCoroutine = StartCoroutine(GetLegendGraphics(urlContainer));
        }
        
        private IEnumerator GetLegendGraphics(LegendUrlContainer urlContainer)
        {
            ShowInactive(urlContainer.LayerNameLegendUrlDictionary.Count == 0);
            if (urlContainer.LayerNameLegendUrlDictionary.Count == 0)
            {
                yield break;
            }

            foreach (string url in urlContainer.LayerNameLegendUrlDictionary.Values)
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

        private void ShowInactive(bool show)
        {
            inactive.gameObject.SetActive(show);
        }
    }
}