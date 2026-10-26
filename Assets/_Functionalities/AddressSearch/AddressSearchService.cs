using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace Netherlands3D.AddressSearch
{
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
    
    public class AddressSearchService : MonoBehaviour
    {
        [Header("PDOK Location API")]
        [SerializeField] private string locationApiBaseUrl = "https://api.pdok.nl/kadaster/location-api/v1";
        [Tooltip("Aantal zoekresultaten.")]
        [SerializeField] private int rows = 5;

        [Tooltip("Aantal karakters voordat een zoekopdracht wordt gestart.")]
        [SerializeField] private int charactersNeededBeforeSearch = 2;
        [Tooltip("Optionele plaatsnaam om aan de zoekopdracht toe te voegen.")]
        [SerializeField] private string searchWithinCity = "";

        [Header("BAG API")]
        [Tooltip("Basis-URL van de BAG OGC API.")]
        [SerializeField] private string bagApiBaseUrl = "https://api.pdok.nl/kadaster/bag/ogc/v2";

        [Header("Camera Controls")]
        [SerializeField] private bool easeCamera = false;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Quaternion targetCameraRotation = Quaternion.Euler(45, 0, 0);
        [SerializeField] public AnimationCurve cameraMoveCurve;
        public UnityEvent<Coordinate> onCoordinateFound = new();
        public UnityEvent<List<string>> onSelectedBuildings = new();
        public event Action<List<SuggestionResult>> SuggestionsReady;
        public event Action SuggestionsCleared;
        public event Action<SuggestionResult> SuggestionAutoSelected;
        private Coroutine suggestionsRoutine;
        private bool autoSelectFirstWhenReady;
        private int searchRequestVersion;
        
        private WorldAnnotation locationPointer;
        private FloatingElement locationElement;
        private CameraService cameraService;
        private AppRootBehaviour appRootBehaviour;
        private WorldUIService worldUIService;
        private const float MaxPixelPointerOffset = 100;
        private const float worldSpaceOffset = 10;
        private Coordinate targetCoordinate;
        private string targetResidentialObjectId;

        private void Start()
        {
            if (!mainCamera) mainCamera = Camera.main;
            
            cameraService = App.Cameras;
            worldUIService  = ServiceLocator.GetService<WorldUIService>();
            appRootBehaviour = App.UIRoot;
            locationPointer = CreateCoordinateLabel(ref locationElement);
            locationPointer.SetSnappingSide(WorldAnnotation.SnappingSide.Above);
            locationPointer.SetColor(new Color(0.000f, 0.275f, 0.600f, 1f)); //blue 900
            locationPointer.SetTextColor(Color.white);
            onCoordinateFound.AddListener(UpdatePointer);
        }

        private void UpdatePointer(Coordinate coordinate)
        {
            locationPointer.EnableInClassList(UtilityClassConstants.HIDDEN, false);
            locationPointer.SetText($"bagid: {targetResidentialObjectId}\nX: {coordinate.easting.ToString("0")}\nY: {coordinate.northing.ToString("0")}".Trim());
        }
        
        private WorldAnnotation CreateCoordinateLabel(ref FloatingElement floatingElement)
        {
            floatingElement = new FloatingElement();
            worldUIService.AddToFloatingElementsContent(floatingElement);
            WorldAnnotation annotation = new WorldAnnotation("");
            annotation.IsCoordinate = true;
            floatingElement.Add(annotation);
            return annotation;
        }

        private void OnDestroy()
        {
            worldUIService.RemoveFromFloatingElementsContent(locationElement);
            locationPointer = null;
            onCoordinateFound.RemoveListener(UpdatePointer);
        }

        void Update()
        {
            UpdateCoordinateTooltip(targetCoordinate, ref locationElement, ref locationPointer);
        }

        private void UpdateCoordinateTooltip(Coordinate coordinate, ref FloatingElement element, ref WorldAnnotation label)
        {
            var screenPos =  cameraService.ActiveCamera.WorldToScreenPoint(coordinate.ToUnity());
            Vector2 panelPos = appRootBehaviour.GetUIPositionFromScreenPosition(screenPos);
            var localPos = worldUIService.FloatingElementsContent.WorldToLocal(panelPos);
            element.SetPosition(localPos);

            var offsetScreenPos = cameraService.ActiveCamera.WorldToScreenPoint(coordinate.ToUnity() + cameraService.ActiveCamera.transform.right * worldSpaceOffset);
            float dist = Mathf.Abs(offsetScreenPos.x - screenPos.x);
            float pixelOffset = Mathf.Min(dist, MaxPixelPointerOffset);
            label.SetLabelOffset(pixelOffset);
        }

        public void FetchSuggestions(string textInput)
        {
            searchRequestVersion++;
            if (suggestionsRoutine != null)
            {
                StopCoroutine(suggestionsRoutine);
                suggestionsRoutine = null;
            }
            autoSelectFirstWhenReady = false;
            if (string.IsNullOrWhiteSpace(textInput) || textInput.Trim().Length <= charactersNeededBeforeSearch)
            {
                SuggestionsCleared?.Invoke();
                return;
            }
            suggestionsRoutine = StartCoroutine(FindSearchSuggestions(textInput.Trim()));
        }
       
        public void FetchSuggestionsForced(string textInput)
        {
            searchRequestVersion++;
            if (suggestionsRoutine != null)
            {
                StopCoroutine(suggestionsRoutine);
                suggestionsRoutine = null;
            }
            if (string.IsNullOrWhiteSpace(textInput))
            {
                SuggestionsCleared?.Invoke();
                return;
            }
            autoSelectFirstWhenReady = true;
            suggestionsRoutine = StartCoroutine(FindSearchSuggestions(textInput.Trim()));
        }
     
        public void SelectSuggestion(string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning("[AddressSearchService] Lege resultaat-ID.");
                return;
            }
            StartCoroutine(ResolveSelectedSuggestion(id));
        }

        private IEnumerator FindSearchSuggestions(string searchTerm)
        {
            int requestVersion = searchRequestVersion;
            string query = searchTerm;
            if (!string.IsNullOrWhiteSpace(searchWithinCity))
                query += " " + searchWithinCity.Trim();

            string url = locationApiBaseUrl.TrimEnd('/') + "/search?q=" + UnityWebRequest.EscapeURL(query) + "&adres%5Bversion%5D=1" + "&limit=" + Mathf.Max(1, rows) + "&f=json";
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 15;

            yield return request.SendWebRequest();

            suggestionsRoutine = null;
            if (requestVersion != searchRequestVersion)
                yield break;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[AddressSearchService] Zoekopdracht mislukt: " + request.error + "\nURL: " + url);
                SuggestionsCleared?.Invoke();
                yield break;
            }

            JSONNode root;
            try
            {
                root = JSON.Parse(request.downloadHandler.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AddressSearchService] Ongeldige JSON-response: " + exception.Message);
                SuggestionsCleared?.Invoke();
                yield break;
            }

            JSONNode features = root["features"];
            if (features == null || !features.IsArray)
            {
                Debug.LogWarning("[AddressSearchService] De zoekresponse bevat " + "geen GeoJSON features-array.\n" + request.downloadHandler.text);
                SuggestionsCleared?.Invoke();
                yield break;
            }

            var suggestions = new List<SuggestionResult>(features.Count);
            for (int i = 0; i < features.Count; i++)
            {
                JSONNode feature = features[i];
                JSONNode properties = feature["properties"];

                string id = ReadString(feature, "id");
                if (string.IsNullOrEmpty(id)) 
                    id = ReadString(properties, "id");

                string label = ReadString(properties, "display_name");

                if (string.IsNullOrEmpty(label))
                    label = ReadString(properties, "weergavenaam");

                if (string.IsNullOrEmpty(label))
                    label = ReadString(properties, "name");

                if (string.IsNullOrEmpty(label))
                    label = id;

                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning("[AddressSearchService] Zoekresultaat zonder ID.");
                    continue;
                }
                suggestions.Add(new SuggestionResult(id, label));
            }

            SuggestionsReady?.Invoke(suggestions);
            if (autoSelectFirstWhenReady)
            {
                autoSelectFirstWhenReady = false;
                if (suggestions.Count > 0)
                {
                    SuggestionAutoSelected?.Invoke(suggestions[0]);
                }
            }
        }
        
        private IEnumerator ResolveSelectedSuggestion(string id)
        {
            string url = bagApiBaseUrl.TrimEnd('/') + "/collections/adres/items/" + UnityWebRequest.EscapeURL(id) + "?f=json";
            JSONNode feature;
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[AddressSearchService] Ophalen van BAG-adres mislukt: " + request.error + "\nURL: " + url);
                    yield break;
                }
                try
                {
                    feature = JSON.Parse(request.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[AddressSearchService] Ongeldige BAG-response: " + exception.Message);
                    yield break;
                }
            }
         
            JSONNode properties = feature["properties"];
            targetResidentialObjectId = ReadString(properties, "adresseerbaar_object_identificatie");
            if (string.IsNullOrEmpty(targetResidentialObjectId))
            {
                Debug.LogWarning("[AddressSearchService] Geen BAG-identificatie van het " + "adresseerbare object gevonden.");
                yield break;
            }

            BagDataService bagDataService = ServiceLocator.GetService<BagDataService>();
            List<string> pandIds = new List<string>();
            yield return bagDataService.GetBAGID(targetResidentialObjectId, result => pandIds = result);
            
            if (pandIds.Count == 0) yield break;

            if (string.IsNullOrEmpty(pandIds[0]))
            {
                Debug.LogWarning("[AddressSearchService] Geen pand-ID gevonden.");
                yield break;
            }
            
            string pandId = pandIds[0];
            BoundingBox buildingBounds = null;
            bool requestFinished = false;

            bagDataService.LoadBuildingBounds(pandId, bounds =>
            {
                buildingBounds = bounds;
                requestFinished = true;
            });

            yield return new WaitUntil(() => requestFinished);

            if (buildingBounds == null)
            {
                Debug.LogWarning("[AddressSearchService] Geen bounding box voor pand " + pandId + " ontvangen.");
                yield break;
            }
            targetCoordinate = buildingBounds.Center;
            if (targetCoordinate.PointsLength == 2) //2D CRS, use the heigtmap to estimate the height.
            {
                targetCoordinate = targetCoordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
                float height = ServiceLocator.GetService<HeightMap>().GetHeight(targetCoordinate);
                targetCoordinate.height = height;
            }
            double targetDistance = buildingBounds.GetSizeMagnitude();
            MoveCameraToCoordinate moveCamera = Camera.main.GetComponent<MoveCameraToCoordinate>();
            if (moveCamera == null)
            {
                Debug.LogWarning("[AddressSearchService] MoveCameraToCoordinate ontbreekt " + "op de Main Camera.");
                yield break;
            }
            onCoordinateFound.Invoke(targetCoordinate);
            moveCamera.LookAtTarget(targetCoordinate, targetDistance);
        }
        
        private static string ReadString(JSONNode node, string key)
        {
            if (node == null)
                return string.Empty;

            JSONNode value = node[key];
            if (value == null || value.IsNull)
                return string.Empty;

            return value.Value;
        }
    }
}