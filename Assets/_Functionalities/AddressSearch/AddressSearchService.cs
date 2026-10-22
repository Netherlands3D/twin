using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Netherlands3D.Coordinates;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.AddressSearch
{
    /// <summary>
    /// Lightweight value that pairs a PDOK suggestion id with its display label.
    /// </summary>
    public readonly struct SuggestionResult
    {
        public readonly string Id;
        public readonly string Label;

        public SuggestionResult(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }

    /// <summary>
    /// UI-agnostic MonoBehaviour that encapsulates all PDOK suggest/lookup/BAG logic and
    /// camera-move behaviour.  Designed to be shared between multiple UI front-ends
    /// (UI Toolkit AddressSearchPanel and the legacy UGUI AddressSearch component).
    /// </summary>
    public class AddressSearchService : MonoBehaviour
    {
        [Tooltip("The WFS endpoint for retrieving BAG information, see: https://www.pdok.nl/geo-services")]
        [SerializeField]
        private string bagWfsEndpoint = "https://service.pdok.nl/lv/bag/wfs/v2_0";

        [Tooltip(
            "The endpoint for retrieving suggestions when looking up addresses, see: https://www.pdok.nl/restful-api")]
        [SerializeField]
        private string locationSuggestionEndpoint = "https://api.pdok.nl/bzk/locatieserver/search/v3_1/suggest";

        [Tooltip("The endpoint for looking up addresses, see: https://www.pdok.nl/restful-api")] [SerializeField]
        private string locationLookupEndpoint = "https://api.pdok.nl/bzk/locatieserver/search/v3_1/lookup";

        [SerializeField] private string searchWithinCity = "Amsterdam";

        [Tooltip("The type of address to filter on, see: https://www.pdok.nl/restful-api")] [SerializeField]
        private string typeFilter = "";

        [SerializeField] private int rows = 5;
        [SerializeField] private int charactersNeededBeforeSearch = 2;

        [Header("Camera Controls")] [SerializeField]
        private bool moveCamera = true;

        [SerializeField] private bool easeCamera = false;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Quaternion targetCameraRotation = Quaternion.Euler(45, 0, 0);
        [SerializeField] public AnimationCurve cameraMoveCurve;

        public UnityEvent<Coordinate> onCoordinateFound = new();
        public UnityEvent<List<string>> onSelectedBuildings = new();

        /// <summary>Raised when a suggest request returns at least one result.</summary>
        public event Action<List<SuggestionResult>> SuggestionsReady;

        /// <summary>Raised when results should be cleared (empty input or too short).</summary>
        public event Action SuggestionsCleared;

        /// <summary>
        /// Raised when FetchSuggestionsForced was called and the first result should be
        /// automatically confirmed (Enter-to-autoselect).
        /// </summary>
        public event Action<SuggestionResult> SuggestionAutoSelected;

        private Coroutine suggestionsRoutine;
        private bool autoSelectFirstWhenReady;

        private void Start()
        {
            if (!mainCamera) mainCamera = Camera.main;
        }

        /// <summary>
        /// Fetch suggestions for <paramref name="textInput"/>.
        /// Clears results when the text is empty or shorter than the configured threshold.
        /// </summary>
        public void FetchSuggestions(string textInput)
        {
            if (suggestionsRoutine != null) StopCoroutine(suggestionsRoutine);

            if (string.IsNullOrWhiteSpace(textInput) || textInput.Length <= charactersNeededBeforeSearch)
            {
                SuggestionsCleared?.Invoke();
                return;
            }

            suggestionsRoutine = StartCoroutine(FindSearchSuggestions(textInput));
        }

        /// <summary>
        /// Fetch suggestions bypassing the min-character threshold; also sets the
        /// auto-select-first flag so that the first result is confirmed automatically.
        /// Used when the user presses Enter with an empty result list.
        /// </summary>
        public void FetchSuggestionsForced(string textInput)
        {
            if (suggestionsRoutine != null) StopCoroutine(suggestionsRoutine);
            autoSelectFirstWhenReady = true;
            suggestionsRoutine = StartCoroutine(FindSearchSuggestions(textInput));
        }

        /// <summary>Perform a geo lookup for <paramref name="id"/> and move the camera.</summary>
        public void SelectSuggestion(string id, string label)
        {
            StartCoroutine(GeoDataLookupRoutine(id));
        }

        private IEnumerator FindSearchSuggestions(string searchTerm)
        {
            string encodedTerm = UnityWebRequest.EscapeURL(searchTerm);
            string cityQuery = searchWithinCity.Length > 0 ? $"and%20{searchWithinCity}%20" : "";
            string typeQuery = typeFilter.Length > 0 ? $"and%20type:{typeFilter}" : "";
            string url = $"{locationSuggestionEndpoint}?q={encodedTerm}%20{cityQuery}{typeQuery}&rows={rows}";

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[AddressSearchService] Suggest request failed.");
                yield break;
            }

            var jsonNode = JSON.Parse(webRequest.downloadHandler.text);
            var docs = jsonNode["response"]["docs"];

            var suggestions = new List<SuggestionResult>(docs.Count);
            for (int i = 0; i < docs.Count; i++)
                suggestions.Add(new SuggestionResult(docs[i]["id"], docs[i]["weergavenaam"]));

            SuggestionsReady?.Invoke(suggestions);

            if (autoSelectFirstWhenReady && suggestions.Count > 0)
            {
                autoSelectFirstWhenReady = false;
                SuggestionAutoSelected?.Invoke(suggestions[0]);
            }
        }

        private IEnumerator GeoDataLookupRoutine(string id)
        {
            string url = $"{locationLookupEndpoint}?id={id}";

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[AddressSearchService] Lookup request failed.");
                yield break;
            }

            var jsonNode = JSON.Parse(webRequest.downloadHandler.text);
            var results = jsonNode["response"]["docs"];
            string centroid = results[0]["centroide_ll"];
            string residentialId = results[0]["adresseerbaarobject_id"];

            Vector3 targetLocation = ExtractUnityLocation(ref centroid);
            onCoordinateFound.Invoke(new Coordinate(targetLocation));

            if (moveCamera)
            {
                var targetPos = new Vector3(targetLocation.x, 300, targetLocation.z - 300);

                if (easeCamera)
                {
                    StartCoroutine(LerpCamera(mainCamera.gameObject, targetPos, targetCameraRotation, 2));
                    yield return new WaitForSeconds(2);
                    StartCoroutine(GetBAGID(residentialId));
                    yield break;
                }

                mainCamera.gameObject.transform.position = targetPos;
            }

            StartCoroutine(GetBAGID(residentialId));
        }

        private IEnumerator GetBAGID(string residentialObjectID)
        {
            string url = $"{bagWfsEndpoint}?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature" +
                         $"&typeName=bag:verblijfsobject&count=100&outputFormat=xml&srsName=EPSG:28992" +
                         $"&filter=<Filter><PropertyIsEqualTo><PropertyName>identificatie</PropertyName>" +
                         $"<Literal>{residentialObjectID}</Literal></PropertyIsEqualTo></Filter>";

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[AddressSearchService] BAG request failed.");
                yield break;
            }

            JSONNode jsonNode = JSON.Parse(webRequest.downloadHandler.text);
            JSONNode bagId = jsonNode["features"][0]["properties"]["pandidentificatie"];

#if UNITY_EDITOR
            Debug.Log($"[AddressSearchService] BAG ID: {bagId}");
#endif

            onSelectedBuildings.Invoke(new List<string> { bagId });
        }

        private IEnumerator LerpCamera(GameObject targetObj, Vector3 endPos, Quaternion endRot, float duration)
        {
            float t = 0;
            Vector3 startPos = targetObj.transform.position;
            Quaternion startRot = targetObj.transform.rotation;

            while (t < duration)
            {
                float eval = cameraMoveCurve.Evaluate(t / duration);
                targetObj.transform.position = Vector3.Lerp(startPos, endPos, eval);
                targetObj.transform.rotation = Quaternion.Lerp(startRot, endRot, eval);
                t += Time.deltaTime;
                yield return null;
            }

            targetObj.transform.position = endPos;
        }

        private static Vector3 ExtractUnityLocation(ref string locationData)
        {
            locationData = locationData.Replace("POINT(", "").Replace(")", "").Replace("\"", "");
            string[] lonLat = locationData.Split(' ');

            double.TryParse(lonLat[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon);
            double.TryParse(lonLat[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat);

            var wgs84 = new Coordinate(CoordinateSystem.WGS84_LatLon, lat, lon);
            return wgs84.ToUnity();
        }
    }
}