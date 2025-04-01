using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using KindMen.Uxios;
using KindMen.Uxios.ExpectedTypesOfResponse;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Functionalities.Wms.UI;
using Netherlands3D.Twin.UI;

namespace Netherlands3D.Functionalities.Wms
{
    public class LegendUrlContainer
    {
        public string GetCapabilitiesUrl;
        public Dictionary<string, string> LayerNameLegendUrlDictionary;
        public int ActiveLayerCount;

        public LegendUrlContainer(string getCapabilitiesUrl, Dictionary<string, string> legendDictionary)
        {
            GetCapabilitiesUrl = getCapabilitiesUrl;
            LayerNameLegendUrlDictionary = legendDictionary;
            ActiveLayerCount = 1; // when creating a new object, we assume it has been created by one layer
        }

        // public void IncrementLayerCount()
        // {
        //     ActiveLayerCount++;
        // }
        //
        // public void DecrementLayerCount()
        // {
        //     ActiveLayerCount--;
        // }
    }

    public class Legend : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private RectTransform inactive;
        [SerializeField] private LegendImage graphicPrefab;

        private LegendClampHeight legendClampHeight;
        private ContentFitterRefresh mainPanelContentFitterRefresh;
        private ICredentialHandler credentialHandler;

        private static readonly Dictionary<string, LegendUrlContainer> legendUrlDictionary = new(); //key: getCapabilities url, Value: legend urls for that GetCapabilities
        private List<string> pendingRequests = new();
        private string requestedLegendUrl; //the url requested to show the legend of
        private string activeLegendUrl; //the currently visible legend url in the panel
        private bool legendActive = false;
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
            credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
        }

        private void OnDestroy()
        {
            credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
        }

        private void AddGraphic(Sprite sprite)
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

        public void ShowLegend(string wmsUrl, bool show)
        {
            Debug.Log("I want so show a legend: " + wmsUrl + "\t" + show);
            legendActive = show;
            mainPanel.SetActive(show);
            if (!show) //no further action needed if we dont want to show anything
                return;

            var getCapabilitiesUrl = OgcWebServicesUtility.CreateGetCapabilitiesURL(wmsUrl, ServiceType.Wms);
            requestedLegendUrl = getCapabilitiesUrl; //se this so we can keep track of the most recent requested url, regardless if we already have the legend urls to download the images from
            if (!legendUrlDictionary.ContainsKey(getCapabilitiesUrl)) //if we don't have the legend url info yet, we need to request it
            {
                Debug.Log("I dont have urls, and will request them: " + wmsUrl);
                AddLegendUrls(getCapabilitiesUrl);
                return; // at the end of the request this function is called again to download the graphics, since we set the requestedLegendUrl, it will request to set those legends active again
            }

            if (activeLegendUrl == requestedLegendUrl)
            {
                Debug.Log("My requested legend is already active" + wmsUrl);
                return; //legend that should be set active is already loaded, so no further action is needed.
            }

            Debug.Log("I need to download my graphics, so I will request credentials: " + wmsUrl);
            credentialHandler.Uri = new Uri(getCapabilitiesUrl);
            credentialHandler.ApplyCredentials();
        }

        private void AddLegendUrls(string layerUrl)
        {
            var getCapabilitiesURL = OgcWebServicesUtility.CreateGetCapabilitiesURL(layerUrl, ServiceType.Wms);

            // if (legendUrlDictionary.ContainsKey(getCapabilitiesURL))
            // {
            //     legendUrlDictionary[getCapabilitiesURL].IncrementLayerCount();
            //     return;
            // }

            if (pendingRequests.Contains(getCapabilitiesURL))
            {
                Debug.Log("My urls are already requested, I will wait for completion: " + layerUrl);
                StartCoroutine(WaitForExistingRequestToComplete(getCapabilitiesURL)); //we need to increment tha amount of active layers once we receive our container
                return;
            }

            if (!OgcWebServicesUtility.IsValidUrl(new Uri(getCapabilitiesURL), RequestType.GetCapabilities))
            {
                Debug.LogError("Bounding boxes not in dictionary, and invalid getCapabilities url provided");
                return;
            }

            Debug.Log("I need to download my urls, so I will request credentials: " + layerUrl);
            credentialHandler.Uri = new Uri(getCapabilitiesURL);
            credentialHandler.ApplyCredentials(); //we assume the credentials are already filled in elsewhere in the application
        }

        private void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            if (auth is FailedOrUnsupported)
                return;

            Debug.Log("I Received credentials: " + uri);
            
            if (!legendUrlDictionary.ContainsKey(uri.ToString()))
            {
                RequestLegendUrls(uri, auth); // successfully requesting the urls will re call the credentialHandler and therefore re-enter this function but the next time with the dictionary key 
            }
            else
            {
                RequestGraphics(uri, auth);
            }
        }

        private IEnumerator WaitForExistingRequestToComplete(string url)
        {
            while (pendingRequests.Contains(url))
            {
                yield return null;
            }

            // legendUrlDictionary[url].IncrementLayerCount();
        }

        public void RemoveLegendUrl(string layerUrl)
        {
            var getCapabilitiesURL = OgcWebServicesUtility.CreateGetCapabilitiesURL(layerUrl, ServiceType.Wms);
            if (legendUrlDictionary.TryGetValue(getCapabilitiesURL, out var container))
            {
                // container.DecrementLayerCount();
                if (container.ActiveLayerCount == 0)
                    legendUrlDictionary.Remove(getCapabilitiesURL); //even though this layer was removed, we might still need this container for other layers
            }
        }

        private void RequestLegendUrls(Uri uri, StoredAuthorization auth)
        {
            Debug.Log("Requesting urls with credentials: " + uri);
            var newContainer = new LegendUrlContainer(uri.ToString(), new()); //already add an empty container to keep track of the amount of layers
            legendUrlDictionary.Add(uri.ToString(), newContainer);
            StartCoroutine(DownloadLegendUrls(uri, auth));
        }

        private IEnumerator DownloadLegendUrls(Uri getCapabilitiesUri, StoredAuthorization auth)
        {
            var config = Config.Default();
            config = auth.AddToConfig(config);
            var promise = Uxios.DefaultInstance.Get<string>(getCapabilitiesUri, config);
            promise.Then(response =>
            {
                Debug.Log("Successfully downloaded legend urls");

                var getCapabilities = new WmsGetCapabilities(getCapabilitiesUri, response.Data as string);
                legendUrlDictionary[getCapabilitiesUri.ToString()].LayerNameLegendUrlDictionary = getCapabilities.GetLegendUrls();
                ShowLegend(requestedLegendUrl, legendActive); //update the legend graphics if we were waiting for the legend urls
            });
            promise.Catch(_ => Debug.LogWarning($"Could not download legends at {getCapabilitiesUri}"));

            yield return Uxios.WaitForRequest(promise);
        }

        private void RequestGraphics(Uri uri, StoredAuthorization auth)
        {
            if (activeLegendUrl != requestedLegendUrl)
            {
                if (runningCoroutine != null)
                    StopCoroutine(runningCoroutine);

                ClearGraphics();

                var urlContainer = legendUrlDictionary[uri.ToString()];
                runningCoroutine = StartCoroutine(DownloadLegendGraphics(urlContainer, auth));
            }
        }


        private IEnumerator DownloadLegendGraphics(LegendUrlContainer urlContainer, StoredAuthorization auth)
        {
            Debug.Log("Downloading " + urlContainer.LayerNameLegendUrlDictionary.Count + "legend graphics " + urlContainer.GetCapabilitiesUrl);
            activeLegendUrl = requestedLegendUrl;
            ShowInactive(urlContainer.LayerNameLegendUrlDictionary.Count == 0);
            if (urlContainer.LayerNameLegendUrlDictionary.Count == 0)
            {
                yield break;
            }

            foreach (string url in urlContainer.LayerNameLegendUrlDictionary.Values)
            {
                var config = new Config() { TypeOfResponseType = new TextureResponse() { Readable = true } };
                config = auth.AddToConfig(config);

                var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(url), config);
                promise.Then(response =>
                {
                    Texture2D tex = response.Data as Texture2D;
                    tex.Apply(false, true);
                    AddGraphic(Sprite.Create(
                        tex,
                        new Rect(0f, 0f, tex.width, tex.height),
                        Vector2.one * 0.5f,
                        100
                    ));
                });

                yield return Uxios.WaitForRequest(promise);
            }
        }

        private void ShowInactive(bool show)
        {
            inactive.gameObject.SetActive(show);
        }
    }
}