using System;
using System.Collections;
using System.Collections.Generic;
using KindMen.Uxios;
using KindMen.Uxios.ExpectedTypesOfResponse;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using UnityEngine;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.UI.Panels;
using RSG;
using UnityEngine.UIElements;

namespace Netherlands3D.Legend
{
    public class LegendBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument; //todo ui-toolkit: use App.UIRoot when this is merged (also for other behaviours that have this reference)
        [SerializeField] private bool log = false;

        private LegendPanel legendPanel;
        private ICredentialHandler credentialHandler;

        // One container per GetCapabilities URL.
        private static readonly Dictionary<string, LegendUrlContainer> containers = new();
        private readonly HashSet<string> pendingGetCapabilitiesRequests = new();

        private Coroutine activeImageDownloadCoroutine;

        private void Awake()
        {
            legendPanel = uiDocument.rootVisualElement.Q<LegendPanel>();
            credentialHandler = GetComponent<ICredentialHandler>();
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
        }

        private void OnDestroy()
        {
            credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
        }

        private void Start()
        {
            legendPanel.SetVisible(false);
        }

        /// <summary>
        /// Called by a layer when it is created or its URL changes.
        /// Registers the layer with the shared container for its GetCapabilities URL
        /// and, if this is the first time we see that URL, kicks off a background
        /// credentials request to start phase 2.
        /// </summary>
        public void RegisterLayer(string wmsUrl, bool isActive)
        {
            if (string.IsNullOrEmpty(wmsUrl))
            {
                Debug.LogError("[WMSLegend] RegisterLayer: url is empty.");
                return;
            }

            var getCapabilitiesUrl = OgcWebServicesUtility.CreateGetCapabilitiesURL(wmsUrl, ServiceType.Wms);

            if (!OgcWebServicesUtility.GetLayerNameFromURL(wmsUrl, out var layerName))
            {
                Debug.LogWarning($"[WMSLegend] Could not extract layer name from: {wmsUrl}");
                layerName = wmsUrl;
            }

            containers[getCapabilitiesUrl].RegisterLayer(layerName, isActive);
            if (log) Debug.Log($"[WMSLegend] Registered layer '{layerName}' (active={isActive}) under {getCapabilitiesUrl}");
        }

        /// <summary>
        /// Called by a layer when it is destroyed or removed from the scene.
        /// </summary>
        public void UnregisterLayer(string wmsUrl)
        {
            if (string.IsNullOrEmpty(wmsUrl)) return;

            var capUrl = OgcWebServicesUtility.CreateGetCapabilitiesURL(wmsUrl, ServiceType.Wms);
            if (!OgcWebServicesUtility.GetLayerNameFromURL(wmsUrl, out var layerName)) return;

            if (containers.TryGetValue(capUrl, out var container))
            {
                container.UnregisterLayer(layerName);
                if (log) Debug.Log($"[WMSLegend] Unregistered layer '{layerName}' from {capUrl}");
            }
        }

        /// <summary>
        /// Called whenever a layer's active/inactive toggle changes.
        /// Updates the stored state and, if the panel is currently showing this
        /// container, starts or refreshes the image download.
        /// </summary>
        public void SetLayerActive(string layerName, bool isActive)
        {
            foreach (var container in containers.Values)
            {
                if (!container.LayerNameLegendUrlDictionary.ContainsKey(layerName))
                    continue;

                container.SetLayerActive(layerName, isActive);
                if (log) Debug.Log($"[WMSLegend] Layer '{layerName}' set active={isActive}");

                if (legendPanel.activeLegendUrlContainer == container && legendPanel.Visible)
                    DownloadMissingImages(container);

                return;
            }

            Debug.LogWarning($"[WMSLegend] SetLayerActive: no container found for layer '{layerName}'");
        }

        private void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            if (auth is FailedOrUnsupported)
                return;

            var url = uri.ToString();
            if (log) Debug.Log($"[WMSLegend] Credentials received for: {url}");


            if (containers.TryGetValue(url, out var container))
                DownloadMissingImages(container);
        }

        public void SetLegendUrls(string containerUrl, Dictionary<string, string> imageUrls)
        {
            var getCapabilitiesUrl = OgcWebServicesUtility.CreateGetCapabilitiesURL(containerUrl, ServiceType.Wms); //ensure the same formatting so the keys match exactly
            if (!containers.TryGetValue(getCapabilitiesUrl, out var container))
            {
                container = new LegendUrlContainer(getCapabilitiesUrl);
                containers.Add(getCapabilitiesUrl, container);
                if (log) Debug.Log($"[WMSLegend] New container created for: {getCapabilitiesUrl}");
            }

            container.PopulateUrls(imageUrls);
            if (log) Debug.Log($"[WMSLegend] Populated {imageUrls.Count} image URLs for {getCapabilitiesUrl}");

            // If the panel is already waiting to show this container, start downloading images now.
            if (legendPanel.activeLegendUrlContainer == container && legendPanel.Visible)
                DownloadMissingImages(container);
        }

        /// <summary>
        /// Shows or hides the legend panel. When showing, sets the container for
        /// the given WMS URL as active and downloads any missing images.
        /// </summary>
        public void ShowLegend(string getMapUrl, bool show)
        {
            legendPanel.SetVisible(show);

            if (!show)
            {
                if (log) Debug.Log("[WMSLegend] Legend hidden.");
                return;
            }

            if (string.IsNullOrEmpty(getMapUrl))
            {
                Debug.LogError("[WMSLegend] ShowLegend: url is empty.");
                return;
            }

            var getCapabilitiesUrl = OgcWebServicesUtility.CreateGetCapabilitiesURL(getMapUrl, ServiceType.Wms);

            if (!containers.TryGetValue(getCapabilitiesUrl, out var container))
            {
                Debug.LogWarning($"[WMSLegend] ShowLegend: no container found for {getCapabilitiesUrl}. Has the layer been registered?");
                return;
            }

            legendPanel.SetContainer(container);
            if (log) Debug.Log($"[WMSLegend] Legend shown for {getCapabilitiesUrl}");

            // If credentials haven't come back yet, the DownloadImageUrls completion will trigger DownloadMissingImages for us.
            if (pendingGetCapabilitiesRequests.Contains(getCapabilitiesUrl))
            {
                if (log) Debug.Log($"[WMSLegend] Waiting for capability URLs before downloading images: {getCapabilitiesUrl}");
                return;
            }

            // Credentials and image URLs are ready: request credentials for image downloads.
            // HandleCredentials will route to DownloadMissingImages since getCapabilitiesUrl is no longer in pendingCapabilitiesRequests.
            credentialHandler.Uri = new Uri(getCapabilitiesUrl);
            if (credentialHandler.Authorization == null || credentialHandler.Authorization is FailedOrUnsupported)
                credentialHandler.ApplyCredentials();
            else
                HandleCredentials(credentialHandler.Uri, credentialHandler.Authorization);
        }

        /// <summary>
        /// Stops any running image download coroutine and starts a fresh one for the given container.
        /// Images already stored are skipped; inactive layers are hidden without downloading.
        /// </summary>
        private void DownloadMissingImages(LegendUrlContainer container)
        {
            if (activeImageDownloadCoroutine != null)
                StopCoroutine(activeImageDownloadCoroutine);

            activeImageDownloadCoroutine = StartCoroutine(DownloadImagesCoroutine(container));
        }

        private IEnumerator DownloadImagesCoroutine(LegendUrlContainer container)
        {
            if (container.LayerNameLegendUrlDictionary.Count == 0)
            {
                if (log) Debug.Log($"[WMSLegend] No image URLs available yet for {container.GetCapabilitiesUrl}.");
                yield break;
            }

            if (log) Debug.Log($"[WMSLegend] Downloading missing images for {container.GetCapabilitiesUrl}");

            foreach (var kv in container.LayerNameLegendUrlDictionary)
            {
                var entry = kv.Value;

                if (!entry.Active)
                    continue; // No texture yet and inactive — nothing to show or download.

                var promise = DownloadImage(container, entry);
                yield return Uxios.WaitForRequest(promise);
            }

            activeImageDownloadCoroutine = null;
        }

        private IPromise<IResponse> DownloadImage(LegendUrlContainer container, LegendUrlContainer.LegendEntry entry)
        {
            var auth = credentialHandler.Authorization;
            var config = new Config { TypeOfResponseType = new TextureResponse { Readable = true } };
            config = auth.AddToConfig(config);
            config = Config.BasedOn(config).WithPayload(new LegendContainerPayload(container, entry.LayerName));

            var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(entry.ImageUrl), config);

            promise.Then(response =>
            {
                var payload = response.Config.GetPayload<LegendContainerPayload>();
                var tex = response.Data as Texture2D;
                tex.Apply(false, true);

                payload.container.RegisterImage(tex, payload.layerName);
                legendPanel.RefreshImage(payload.layerName, tex);

                if (log) Debug.Log($"[WMSLegend] Downloaded image for layer '{payload.layerName}'");
            });

            promise.Catch(ex =>
                Debug.LogWarning($"[WMSLegend] Failed to download image for '{entry.LayerName}': {ex.Message}"));

            return promise;
        }
    }
}