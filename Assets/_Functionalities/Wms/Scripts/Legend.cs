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
using RSG;
using KeyValuePair = Netherlands3D.Twin.UI.KeyValuePair;

namespace Netherlands3D.Functionalities.Wms
{
    public class LegendUrlContainer
    {
        public string GetCapabilitiesUrl;
        public Dictionary<string, LegendEntry> LayerNameLegendUrlDictionary = new();
        public int ActiveLayerCount;
        
        public sealed class LegendEntry
        {
            public readonly string LayerName;
            public readonly string Url;

            public LegendImage Image;
            public bool Active;

            public LegendEntry(string layerName, string url)
            {
                LayerName = layerName;
                Url = url;
                Active = true;
            }
            
            public void SetImage(LegendImage image) => Image = image;
            public void SetActive(bool active) => Active = active;
        }

        public LegendUrlContainer(string getCapabilitiesUrl, Dictionary<string, string> legendDictionary)
        {
            GetCapabilitiesUrl = getCapabilitiesUrl;
            foreach(KeyValuePair<string, string> kv in  legendDictionary)
                LayerNameLegendUrlDictionary.Add(kv.Key, new LegendEntry(kv.Key, kv.Value));
            ActiveLayerCount = 1; // when creating a new object, we assume it has been created by one layer
        }

        public void IncrementLayerCount()
        {
            ActiveLayerCount++;
        }

        public void DecrementLayerCount()
        {
            ActiveLayerCount--;
        }

        public void RegisterImage(LegendImage legendImage, string layerName)
        {
            LayerNameLegendUrlDictionary[layerName].SetImage(legendImage);
        }

        public void RegisterActiveLayer(string layerName, bool active)
        {
            LayerNameLegendUrlDictionary[layerName].Active = active;
        }
    }

    public class Legend : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private RectTransform inactive;
        [SerializeField] private LegendImage graphicPrefab;
        [SerializeField] private bool log = false;

        private LegendClampHeight legendClampHeight;
        private ContentFitterRefresh mainPanelContentFitterRefresh;
        private ICredentialHandler credentialHandler;

        private static readonly Dictionary<string, LegendUrlContainer> legendUrlDictionary = new(); //key: getCapabilities url, Value: legend urls for that GetCapabilities
        private Dictionary<string, Coroutine> pendingUrlRequests = new();
        private string requestedLegendUrl; //the url requested to show the legend of
        private string activeLegendUrl; //the currently visible legend url in the panel
        private bool legendActive = false;
        private Coroutine runningCoroutine;

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

        public void RegisterUrl(string wmsUrl, bool activeLayer)
        {
            if (string.IsNullOrEmpty(wmsUrl))
            {
                Debug.LogError("Url you are trying to register is empty");
                return;
            }
            
            var getCapabilitiesUrl = OgcWebServicesUtility.CreateGetCapabilitiesURL(wmsUrl, ServiceType.Wms);
            if (log) Debug.Log("registering Url: " + getCapabilitiesUrl);
            if (legendUrlDictionary.TryGetValue(getCapabilitiesUrl, out var container)) //if we don't have the legend url info yet, we need to request it
            {
                if(OgcWebServicesUtility.GetLayerNameFromURL(wmsUrl, out var layerName))
                    container.RegisterActiveLayer(layerName, activeLayer);
                
                container.IncrementLayerCount();
                return;
            }

            if (pendingUrlRequests.ContainsKey(getCapabilitiesUrl))
            {
                if (log) Debug.Log("urls are already requested, waiting for completion: " + getCapabilitiesUrl);
                StartCoroutine(WaitForExistingRequestToComplete(getCapabilitiesUrl)); //we need to increment tha amount of active layers once we receive our container
                return;
            }

            if (log) Debug.Log("No urls found, requesting urls: " + wmsUrl);
            if (!OgcWebServicesUtility.IsValidUrl(new Uri(getCapabilitiesUrl), RequestType.GetCapabilities))
            {
                Debug.LogError("Bounding boxes not in dictionary, and invalid getCapabilities url provided");
                return;
            }

            if (log) Debug.Log("Requesting credentials: " + getCapabilitiesUrl);
            pendingUrlRequests.Add(getCapabilitiesUrl, null); //we cannot create the coroutine until we have the credentials
            credentialHandler.Uri = new Uri(getCapabilitiesUrl);
            credentialHandler.ApplyCredentials(); //we assume the credentials are already filled in elsewhere in the application
        }

        public void ShowLegend(string wmsUrl, bool show)
        {
            if (log) Debug.Log("Setting legend active: " + wmsUrl + "\t" + show);
            legendActive = show;
            mainPanel.SetActive(show);
            if (!show) //no further action needed if we dont want to show anything
                return;
            
            if (string.IsNullOrEmpty(wmsUrl))
            {
                Debug.LogError("Url you are trying to show is empty");
                return;
            }

            var getCapabilitiesUrl = OgcWebServicesUtility.CreateGetCapabilitiesURL(wmsUrl, ServiceType.Wms);
            requestedLegendUrl = getCapabilitiesUrl; //se this so we can keep track of the most recent requested url, regardless if we already have the legend urls to download the images from

            if (activeLegendUrl == requestedLegendUrl)
            {
                if (log) Debug.Log("Requested legend is already active" + wmsUrl);
                return; //legend that should be set active is already loaded, so no further action is needed.
            }


            if (log) Debug.Log("Should download graphics, requesting credentials: " + wmsUrl);
            credentialHandler.Uri = new Uri(getCapabilitiesUrl);
            if(credentialHandler.Authorization == null || credentialHandler.Authorization is FailedOrUnsupported)
                credentialHandler.ApplyCredentials();
            else
                HandleCredentials(credentialHandler.Uri, credentialHandler.Authorization);
        }

        public void UnregisterUrl(string layerUrl)
        {
            var getCapabilitiesURL = OgcWebServicesUtility.CreateGetCapabilitiesURL(layerUrl, ServiceType.Wms);
            if (legendUrlDictionary.TryGetValue(getCapabilitiesURL, out var container))
            {
                container.DecrementLayerCount();
                if (container.ActiveLayerCount == 0)
                {
                    if (log) Debug.Log("Removing legend urls");
                    legendUrlDictionary.Remove(getCapabilitiesURL); //even though this layer was removed, we might still need this container for other layers
                }
            }
        }

        private void HandleCredentials(Uri getCapabilitiesUri, StoredAuthorization auth)
        {
            if (auth is FailedOrUnsupported)
                return;

            if(log) Debug.Log("Received credentials: " + getCapabilitiesUri);

            if (pendingUrlRequests.TryGetValue(getCapabilitiesUri.ToString(), out var activeUrlCoroutine))
            {
                if (activeUrlCoroutine != null) //coroutine is still running, no need to start a new one, and we cannot request the graphics yet 
                    return;
                
                // we still need to actually request the urls now that we have the credentials, not just block external objects from performing the same request multiple times
                if(log) Debug.Log("requesting legend urls with credentials");
                RequestLegendUrls(getCapabilitiesUri, auth); // successfully requesting the urls will re call the credentialHandler and therefore re-enter this function but the next time with the dictionary key 
            }
            else
            {
                RequestGraphics(getCapabilitiesUri, auth);
            }
        }

        private IEnumerator WaitForExistingRequestToComplete(string getCapabilitiesUrl)
        {
            yield return new WaitUntil(() => !pendingUrlRequests.ContainsKey(getCapabilitiesUrl));
            legendUrlDictionary[getCapabilitiesUrl].IncrementLayerCount();
        }

        private void RequestLegendUrls(Uri getCapabilitiesUri, StoredAuthorization auth)
        {
            if(log) Debug.Log("Requesting urls with credentials: " + getCapabilitiesUri);
            pendingUrlRequests[getCapabilitiesUri.ToString()] = StartCoroutine(DownloadLegendUrls(getCapabilitiesUri, auth));
        }

        private IEnumerator DownloadLegendUrls(Uri getCapabilitiesUri, StoredAuthorization auth)
        {
            var config = Config.Default();
            config = auth.AddToConfig(config);
            var promise = Uxios.DefaultInstance.Get<string>(getCapabilitiesUri, config);
            promise.Then(response =>
            {
                var getCapabilities = new WmsGetCapabilities(getCapabilitiesUri, response.Data as string);
                var urls = getCapabilities.GetLegendUrls();
                var newContainer = new LegendUrlContainer(getCapabilitiesUri.ToString(), urls); //already add an empty container to keep track of the amount of layers
                legendUrlDictionary.Add(newContainer.GetCapabilitiesUrl, newContainer);
                var a = pendingUrlRequests.Remove(newContainer.GetCapabilitiesUrl);
                if(log) Debug.Log("Successfully downloaded " + urls.Count + " legend urls");
                ShowLegend(requestedLegendUrl, legendActive); //update the legend graphics if we were waiting for the legend urls
            });
            promise.Catch(_ => Debug.LogWarning($"Could not download legends at {getCapabilitiesUri}"));

            yield return Uxios.WaitForRequest(promise);
        }

        private void RequestGraphics(Uri getCapabilitiesUri, StoredAuthorization auth)
        {
            if (activeLegendUrl == requestedLegendUrl) return;

            if (runningCoroutine != null)
            {
                StopCoroutine(runningCoroutine);
            }

            ClearGraphics();

            if (!legendUrlDictionary.ContainsKey(getCapabilitiesUri.ToString()))
            {
                Debug.LogError("Could not find legend urls for the given WMS service " + getCapabilitiesUri);
                return;
            }

            var urlContainer = legendUrlDictionary[getCapabilitiesUri.ToString()];
            runningCoroutine = StartCoroutine(DownloadLegendGraphics(urlContainer, auth));
        }


        private IEnumerator DownloadLegendGraphics(LegendUrlContainer urlContainer, StoredAuthorization auth)
        {
            if(log) Debug.Log("Downloading " + urlContainer.LayerNameLegendUrlDictionary.Count + "legend graphics " + urlContainer.GetCapabilitiesUrl);
            activeLegendUrl = requestedLegendUrl;
            ShowInactive(urlContainer.LayerNameLegendUrlDictionary.Count == 0);
            if (urlContainer.LayerNameLegendUrlDictionary.Count == 0)
            {
                yield break;
            }

            foreach (var kv in urlContainer.LayerNameLegendUrlDictionary)
            {
                //is the layer inactive or the image already loaded then do not request the download
                if(kv.Value.Active == false || kv.Value.Image != null)
                    continue;
                
                var promise = RequestAndCreateImage(urlContainer, kv.Value, auth);
                yield return Uxios.WaitForRequest(promise);
            }
        }

        private Promise<IResponse> RequestAndCreateImage(LegendUrlContainer urlContainer, LegendUrlContainer.LegendEntry entry, StoredAuthorization auth)
        {
            var config = new Config() { TypeOfResponseType = new TextureResponse() { Readable = true } };
            config = auth.AddToConfig(config);
                
            var configWithPayload = Config.BasedOn(config);
            Uri uri = new Uri(entry.Url);
            configWithPayload = configWithPayload.WithPayload(new LegendContainerPayload(urlContainer, entry.LayerName));
               
            var promise = Uxios.DefaultInstance.Get<Texture2D>(uri, configWithPayload);
            promise.Then(response =>
            {
                var payload = response.Config.GetPayload<LegendContainerPayload>();
                LegendUrlContainer container = payload.container;
                string layerName = payload.layerName;
                    
                Texture2D tex = response.Data as Texture2D;
                tex.Apply(false, true);
                AddGraphic(container, layerName, Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    Vector2.one * 0.5f,
                    100
                ));
            });
            return promise;
        }

        private void AddGraphic(LegendUrlContainer container, string layerName, Sprite sprite)
        {
            LegendImage image = Instantiate(graphicPrefab, graphicPrefab.transform.parent);
            image.gameObject.SetActive(true);
            image.SetSprite(sprite);
            container.RegisterImage(image, layerName);

            legendClampHeight.AdjustRectHeight();
            mainPanelContentFitterRefresh.RefreshContentFitters();
        }

        private void ClearGraphics()
        {
            if(activeLegendUrl == null) return;

            if (legendUrlDictionary.TryGetValue(activeLegendUrl, out var container))
            {
                foreach(KeyValuePair<string, LegendUrlContainer.LegendEntry> entry in container.LayerNameLegendUrlDictionary)
                {
                    if (entry.Value.Image != null)
                    {
                        Destroy(entry.Value.Image.gameObject);
                        entry.Value.Image = null;
                    }
                }
            }
        }

        public void ToggleLayer(string layerName, bool isActive)
        {
            if(activeLegendUrl == null) return;
            
            if (legendUrlDictionary.TryGetValue(activeLegendUrl, out var container))
            {
                if(!container.LayerNameLegendUrlDictionary.ContainsKey(layerName)) return;
                
                LegendUrlContainer.LegendEntry entry = container.LayerNameLegendUrlDictionary[layerName]; 
                if(entry.Image != null)
                    entry.Image.gameObject.SetActive(isActive);
                else if(isActive)
                    RequestAndCreateImage(container, entry, credentialHandler.Authorization);
            }
        }

        private void ShowInactive(bool show)
        {
            inactive.gameObject.SetActive(show);
        }
    }
    
    public record LegendContainerPayload(LegendUrlContainer container, string layerName)
    {
        public LegendUrlContainer container { get; } = container;
        public string layerName { get; } = layerName;
    }
}