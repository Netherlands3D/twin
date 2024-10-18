using System;
using System.Collections;
using System.Linq;
using System.Text;
using Netherlands3D.Indicators.Dossiers;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Netherlands3D.Indicators.Dossiers.DataLayers;
using Netherlands3D.Indicators.Esri;

namespace Netherlands3D.Indicators
{
    [CreateAssetMenu(menuName = "Netherlands3D/Dossier", fileName = "DossierSO", order = 0)]
    public class DossierSO : ScriptableObject
    {
        [Serializable]
        public struct ImportRequest
        {
            public string iri;
        }

        /// <summary>
        /// We keep track of the current state the Dossier System.
        ///
        /// Some elements, such as the sidebar, need to know the latest state of the system because they need to have
        /// an initial state after being instantiated. The DossierSystemState provides a way to communicate the latest
        /// state when it comes to loading a dossier.
        /// </summary>
        [Serializable]
        public enum DossierSystemState
        {
            Importing,
            ImportingFailed,
            Imported,
            Opening,
            Opened,
            FailedToOpen,
            Closed
        }

        [SerializeField]
        [Tooltip("if the DossierUriTemplate contains a variable {baseUri}, it will be replaced by this value.")]
        public string baseUri = "";
        
        [SerializeField]
        [Tooltip("Contains the URI Template where to find the dossier's JSON file. The dossier id can be inserted using a variable {id}, a dynamic base URI can also be injected using the variable {baseUri}.")]
        private string dossierUriTemplate = "{baseUri}/dossier/{id}";

        [Tooltip("Contains the URI Template where to import a dossier. A dynamic base URI can be injected using the variable {baseUri}.")]
        [SerializeField] private string importUriTemplate = "{baseUri}/dossiers/import";

        [SerializeField]
        private string apiKey = "";
        public string ApiKey
        {
            get => apiKey;
            set => apiKey = value;
        }

        public UnityEvent onImporting = new();
        public UnityEvent onImportingFailed = new();
        public UnityEvent<string> onImport = new();
        public UnityEvent onOpening = new();
        public UnityEvent<Dossier> onOpen = new();
        public UnityEvent<Variant?> onSelectedVariant = new();
        public UnityEvent<ProjectArea?> onSelectedProjectArea = new();
        public UnityEvent<DataLayer?> onSelectedDataLayer = new();
        public UnityEvent<Uri> onLoadMapOverlayFrame = new();
        public UnityEvent onFailedToOpen = new();
        public UnityEvent onClose = new();
        public UnityEvent<FeatureCollection> onLoadedProjectArea = new();
        
        [NonSerialized]
        private DossierSystemState dossierSystemState = DossierSystemState.Closed;
        public DossierSystemState State => dossierSystemState;

        public Dossier? Data { get; private set; }
        public Variant? ActiveVariant { get; private set; }

        private ProjectArea? activeProjectArea;
        public ProjectArea? ActiveProjectArea
        {
            get => activeProjectArea;
            set
            {
                onSelectedProjectArea.Invoke(value);
                activeProjectArea = value;
            }
        }

        private DataLayer? selectedDataLayer;

        public DataLayer? SelectedDataLayer
        {
            get => selectedDataLayer;
            set
            {
                // if the given value is not part of the active variant, never mind; that is an illegal action and
                // we ignore it.
                if (
                    ActiveVariant.HasValue == false 
                    || (value.HasValue && ActiveVariant.Value.maps.ContainsValue(value.Value) == false)
                ) {
                    return;
                }

                onSelectedDataLayer.Invoke(value);
                selectedDataLayer = value;
                
                if (value.HasValue == false || value.Value.frames.Count == 0) return;
                
                // since we do not support multiple frames at the moment, we cheat and always load the first
                var firstFrame = value.Value.frames.First();
                var firstFrameMapUrl = AppendApiKeyToURL(firstFrame.map);
                
                // We parse the frame map data
                onLoadMapOverlayFrame.Invoke(firstFrameMapUrl);
            }
        }

        private Uri AppendApiKeyToURL(Uri url)
        {
            if (string.IsNullOrEmpty(apiKey)) return url;

            var uriBuilder = new UriBuilder(url);
            uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) 
                ? $"code={apiKey}" 
                : string.Concat(uriBuilder.Query, $"&code={apiKey}");
            
            return uriBuilder.Uri;
        }

        public IEnumerator LoadMapDataAsync(Frame frame)
        {
            var uriWithCode = AppendApiKeyToURL(frame.data);
            UnityWebRequest www = UnityWebRequest.Get(uriWithCode);
            yield return www.SendWebRequest();
            Debug.Log($"<color=green>Getting mapdata from {uriWithCode}</color>");

            if (www.result != UnityWebRequest.Result.Success)
            {
                onFailedToOpen.Invoke();
                Debug.Log($"<color=red>Failed to load mapdata from {uriWithCode}</color>");
                Debug.LogError(www.error);
                yield break;
            }

            var mapAsciiData = www.downloadHandler.text;
            var mapData = new EsriRasterData();
            mapData.ParseASCII(mapAsciiData);
            frame.mapData = mapData;             
        }

        public IEnumerator Import(string sourceIri)
        {
            dossierSystemState = DossierSystemState.Importing;
            onImporting.Invoke();
            string url = AssembleImportUri();
            Debug.Log($"<color=orange>Importing dossier with iri {sourceIri} from {url}</color>");

            var www = PostAsJson(url, new ImportRequest { iri = sourceIri });
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onImportingFailed.Invoke();
                dossierSystemState = DossierSystemState.ImportingFailed;
                Debug.Log($"<color=red>Failed to import dossier from {sourceIri}</color>");
                Debug.LogError(www.error);
                yield break;
            }

            var json = www.downloadHandler.text;
            Debug.Log($"<color=orange>Received response data when importing dossier</color>");
            Debug.Log(json);
            
            Dossier dossier;
            try
            {
                dossier = JsonConvert.DeserializeObject<Dossier>(json);
            }
            catch (Exception e)
            {
                Debug.Log($"<color=red>Failed to deserialize dossier while importing</color>");
                Debug.LogError(e.Message);
                dossierSystemState = DossierSystemState.ImportingFailed;
                onImportingFailed.Invoke();
                yield break;
            }
            
            dossierSystemState = DossierSystemState.Imported;
            
            onImport.Invoke(dossier.id);
        }

        private UnityWebRequest PostAsJson(string url, object data)
        {
            var requestContent = JsonConvert.SerializeObject(data);
            var www = new UnityWebRequest(url, "POST");
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestContent));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            return www;
        }

        public IEnumerator Open(string dossierId)
        {
            dossierSystemState = DossierSystemState.Opening;
            onOpening.Invoke();
            string url = AssembleFetchUri(dossierId);
            Debug.Log($"<color=orange>Loading dossier with id {dossierId} from {url}</color>");
            Close();

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onFailedToOpen.Invoke();
                dossierSystemState = DossierSystemState.FailedToOpen;
                Debug.Log($"<color=red>Failed to load dossier from {url}</color>");
                Debug.LogError(www.error);
                yield break;
            }

            var json = www.downloadHandler.text;
            Debug.Log($"<color=orange>Received dossier data from {url}</color>");
            Debug.Log(json);

            Dossier dossier;
            try
            {
                dossier = JsonConvert.DeserializeObject<Dossier>(json);
            }
            catch (Exception e)
            {
                Debug.Log($"<color=red>Failed to deserialize dossier from {url}</color>");
                Debug.LogError(e.Message);
                dossierSystemState = DossierSystemState.FailedToOpen;
                onFailedToOpen.Invoke();
                yield break;
            }

            Debug.Log($"<color=green>Loaded dossier with id {dossier.id} from {url}</color>");

            OpenDossier(dossier);
        }

        private void OpenDossier(Dossier dossier)
        {
            Data = dossier;
            dossierSystemState = DossierSystemState.Opened;
            onOpen.Invoke(dossier);

            SelectVariant(dossier.variants.FirstOrDefault());
        }

        public void Close()
        {
            onClose.Invoke();
            dossierSystemState = DossierSystemState.Closed;
            Data = null;
            SelectVariant(null);
            ActiveProjectArea = null;
        }

        public void SelectVariant(Variant? variant)
        {
            ActiveVariant = variant;
            onSelectedVariant.Invoke(variant);

            if (variant.HasValue != false) return;
        }

        public IEnumerator LoadProjectAreaGeometry(Variant variant)
        {
            var featureCollection = new FeatureCollection(
                variant.areas.Select(area => new Feature(area.geometry, null, area.id)).ToList()
            );
            if (Data.HasValue)
            {
                featureCollection.CRS = Data.Value.crs;
            }
            
            onLoadedProjectArea.Invoke(featureCollection);
            yield return null;
        }

        private string AssembleFetchUri(string dossierId)
        {
            var url = dossierUriTemplate
                .Replace("{baseUri}", baseUri)
                .Replace("{id}", dossierId);
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                var uriBuilder = new UriBuilder(url);
                uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) 
                    ? $"code={apiKey}" 
                    : string.Concat(uriBuilder.Query, $"&code={apiKey}");
                url = uriBuilder.ToString();
            }

            return url;
        }

        private string AssembleImportUri()
        {
            var url = importUriTemplate
                .Replace("{baseUri}", baseUri);
            
            if (!string.IsNullOrEmpty(apiKey))
            {
                var uriBuilder = new UriBuilder(url);
                uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) 
                    ? $"code={apiKey}" 
                    : string.Concat(uriBuilder.Query, $"&code={apiKey}");
                url = uriBuilder.ToString();
            }

            return url;
        }
    }
}