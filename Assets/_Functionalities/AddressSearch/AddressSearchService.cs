using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        public readonly string DisplayLabel;
        public readonly string CollectionId;
        public readonly float Score;
        public readonly BoundingBox BoundingBox;
        public readonly bool HasCoordinates;

        public SuggestionResult(string id, string label, string displayLabel, string collectionId, float score, BoundingBox boundingBox, bool hasCoordinates)
        {
            Id = id;
            Label = label;
            DisplayLabel = label + ", " + displayLabel;
            CollectionId = collectionId;
            Score = score;
            BoundingBox = boundingBox;
            HasCoordinates = hasCoordinates;
        }
    }

    public class AddressSearchService : MonoBehaviour
    {
        [Header("PDOK Location API")]
        private string locationApiBaseUrl = "https://api.pdok.nl/kadaster/location-api/v1";
        private int rows =1000;
        private int maxResultsPerCollection = 10;
        private float minimumScore = 0;
        private int charactersNeededBeforeSearch = 2;

        [Header("BAG API")]
        [SerializeField] private string bagApiBaseUrl = "https://api.pdok.nl/kadaster/bag/ogc/v2";

        [Header("Camera Controls")]
        [SerializeField] private bool easeCamera;
        [SerializeField] private Camera mainCamera;
        private double minSizeMagnitudeDistance = 20; //inrichtingselementen have no bbox size magnitudes

        public UnityEvent<Coordinate> onCoordinateFound = new();
        public event Action<List<SuggestionResult>> SuggestionsReady;
        public event Action SuggestionsCleared;
        public event Action<SuggestionResult> SuggestionAutoSelected;

        private static readonly string[] CollectionIds =
        {
            "adres", "functioneel_gebied", "gebouw", "gemeentegebied", "geografisch_gebied",
            "inrichtingselement", "perceel", "plaats", "provinciegebied", "spoorbaandeel",
            "waterdeel", "wegdeel", "woonplaats"
        };

        // Volgorde waarin collecties in de lijst verschijnen (eerst grote gebieden en adressen).
        private static readonly string[] CollectionPriorityOrder =
        {
            "woonplaats", "gemeentegebied", "provinciegebied", "plaats", "adres",
            "geografisch_gebied", "perceel", "gebouw", "wegdeel", "functioneel_gebied",
            "inrichtingselement", "spoorbaandeel", "waterdeel"
        };

        // Alle PDOK Location API data is CRS84 (lon, lat), dat is gelijk aan WGS84 maar dan met de volgorde lon/lat.
        private const string Crs84 = "http://www.opengis.net/def/crs/OGC/1.3/CRS84";
        private const float MaxPixelPointerOffset = 100f;
        private const float WorldSpaceOffset = 10f;
        private const float DefaultLocationCameraDistance = 100f;

        // Ontdubbelde resultaten, key = "collectie:naam" (lowercase). Zelfde naam binnen een collectie telt als één resultaat.
        private readonly Dictionary<string, SuggestionResult> cachedSuggestions = new();
        // Dezelfde resultaten in weergavevolgorde voor de UI.
        private readonly List<SuggestionResult> orderedSuggestions = new();

        private Coroutine suggestionsRoutine;
        private bool autoSelectFirstWhenReady;
        private int searchRequestVersion;
        private WorldAnnotation locationPointer;
        private FloatingElement locationElement;
        private CameraService cameraService;
        private AppRootBehaviour appRootBehaviour;
        private WorldUIService worldUIService;
        private Coordinate targetCoordinate;
        private string targetId;
        
        private static readonly Dictionary<string, string> CollectionDisplayNames = new()
        {
            { "adres", "Adres" },
            { "woonplaats", "Woonplaats" },
            { "gemeentegebied", "Gemeente" },
            { "provinciegebied", "Provincie" },
            { "plaats", "Plaats" },
            { "perceel", "Perceel" },
            { "gebouw", "Gebouw" },
            { "wegdeel", "Weg" },
            { "waterdeel", "Water" },
            { "spoorbaandeel", "Spoor" },
            { "geografisch_gebied", "Gebied" },
            { "functioneel_gebied", "Functioneel gebied" },
            { "inrichtingselement", "Inrichtingselement" },
        };

        public static string GetDisplayName(string collectionId) => CollectionDisplayNames.TryGetValue(collectionId, out var name) ? name : collectionId;

        private static string MakeKey(string collectionId, string label) => collectionId + ":" + label.Trim().ToLowerInvariant();

        private static int GetPriority(string collectionId)
        {
            int index = Array.IndexOf(CollectionPriorityOrder, collectionId);
            return index < 0 ? int.MaxValue : index;
        }

        private void Start()
        {
            if (!mainCamera) mainCamera = Camera.main;

            cameraService = App.Cameras;
            worldUIService = ServiceLocator.GetService<WorldUIService>();
            appRootBehaviour = App.UIRoot;

            locationPointer = CreateCoordinateLabel(ref locationElement);
            locationPointer.SetSnappingSide(WorldAnnotation.SnappingSide.Above);
            locationPointer.SetColor(new Color(0f, 0.275f, 0.6f, 1f));
            locationPointer.SetTextColor(Color.white);
            HidePointer();

            ServiceLocator.GetService<ToolService>().GetTool(ToolType.Search).onClose.AddListener(HidePointer);
            onCoordinateFound.AddListener(UpdatePointer);
        }

        private void OnEnable()
        {
            HidePointer();
        }

        public void FetchSuggestions(string textInput)
        {
            StartSearch(textInput, false);
        }

        public void FetchSuggestionsForced(string textInput)
        {
            StartSearch(textInput, true);
        }

        private void StartSearch(string textInput, bool autoSelectFirst)
        {
            searchRequestVersion++;
            if (suggestionsRoutine != null) StopCoroutine(suggestionsRoutine);

            suggestionsRoutine = null;
            autoSelectFirstWhenReady = autoSelectFirst;

            if (string.IsNullOrWhiteSpace(textInput) || (!autoSelectFirst && textInput.Trim().Length <= charactersNeededBeforeSearch))
            {
                ClearSuggestions();
                return;
            }

            ClearSuggestions();
            suggestionsRoutine = StartCoroutine(FindSearchSuggestions(textInput.Trim(), searchRequestVersion));
        }

        private IEnumerator FindSearchSuggestions(string searchTerm, int requestVersion)
        {
            string query = searchTerm;
            
            var uniqueSuggestions = new Dictionary<string, SuggestionResult>();
            // Query each collection separately so every result keeps its correct collection ID.
            foreach (string collectionId in CollectionIds)
            {
                if (requestVersion != searchRequestVersion) yield break;

                string url = locationApiBaseUrl.TrimEnd('/') + "/search?q=" + UnityWebRequest.EscapeURL(query)
                    + "&" + UnityWebRequest.EscapeURL(collectionId) + "%5Bversion%5D=1"
                    + "&limit=" + Mathf.Max(maxResultsPerCollection, rows)
                    + "&crs=" + UnityWebRequest.EscapeURL(Crs84)
                    + "&f=json";

                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.timeout = 15;
                yield return request.SendWebRequest();

                if (requestVersion != searchRequestVersion) yield break;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[AddressSearchService] Zoekopdracht voor collectie '" + collectionId + "' mislukt: " + request.error);
                    continue;
                }

                JSONNode root;
                try
                {
                    root = JSON.Parse(request.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[AddressSearchService] Ongeldige JSON voor collectie '" + collectionId + "': " + exception.Message);
                    continue;
                }

                JSONNode features = root["features"];
                if (features == null || !features.IsArray) continue;

                for (int i = 0; i < features.Count; i++)
                {
                    JSONNode feature = features[i];
                    JSONNode properties = feature["properties"];

                    string id = ReadString(feature, "id");
                    if (string.IsNullOrWhiteSpace(id)) id = ReadString(properties, "id");
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    // Reject results without a score and every result below the configured threshold.
                    if (!TryReadScore(properties, out float score) || score < minimumScore) continue;

                    string label = ReadString(properties, "display_name");
                    if (string.IsNullOrWhiteSpace(label)) label = ReadString(properties, "weergavenaam");
                    if (string.IsNullOrWhiteSpace(label)) label = ReadString(properties, "name");
                    if (string.IsNullOrWhiteSpace(label)) label = id;

                    Coordinate coordinate = default;
                    BoundingBox bbox = null;
                    JSONNode bboxNode = feature["bbox"];

                    bool hasCoordinates = false;
                    bool hasBounds = false;
                    if (bboxNode != null && bboxNode.IsArray && bboxNode.Count >= 4)
                    {
                        double minLon = bboxNode[0].AsDouble;
                        double minLat = bboxNode[1].AsDouble;
                        double maxLon = bboxNode[2].AsDouble;
                        double maxLat = bboxNode[3].AsDouble;

                        bbox = new BoundingBox(new Coordinate(CoordinateSystem.WGS84_LatLon, minLat, minLon), new Coordinate(CoordinateSystem.WGS84_LatLon, maxLat, maxLon));
                        hasBounds = true;
                        hasCoordinates = true;
                    }
                    
                    JSONNode geometry = feature["geometry"];
                    JSONNode coords = geometry != null ? geometry["coordinates"] : null;
                    
                    if (geometry != null && string.Equals(ReadString(geometry, "type"), "Point", StringComparison.OrdinalIgnoreCase) && coords != null && coords.IsArray && coords.Count >= 2)
                    {
                        double lon = coords[0].AsDouble;
                        double lat = coords[1].AsDouble;

                        var point = new Coordinate(CoordinateSystem.WGS84_LatLon, lat, lon);
                        if(!hasBounds)
                            bbox = new BoundingBox(point, point);
                        hasCoordinates = true;
                    }
                    var result = new SuggestionResult(id, label, GetDisplayName(collectionId), collectionId, score, bbox, hasCoordinates);
                    string key = MakeKey(collectionId, label);
                    if (!uniqueSuggestions.TryGetValue(key, out SuggestionResult existing) || result.Score > existing.Score)
                    {
                        uniqueSuggestions[key] = result;
                    }
                }
            }

            if (requestVersion != searchRequestVersion) yield break;

            //order by collection then score
             List<SuggestionResult> best = uniqueSuggestions.Values
                 .GroupBy(result => result.CollectionId)
                 .SelectMany(group => group.OrderByDescending(result => result.Score).Take(maxResultsPerCollection))
                 .OrderBy(result => GetPriority(result.CollectionId))
                 .ThenByDescending(result => result.Score)
                 .Take(Mathf.Max(1, rows))
                 .ToList();
         
            //order by score then collection
            // List<SuggestionResult> best = uniqueSuggestions.Values
            //     .GroupBy(result => result.CollectionId)
            //     .SelectMany(group => group.OrderByDescending(result => result.Score).Take(maxResultsPerCollection))
            //     .OrderByDescending(result => result.Score)
            //     .ThenBy(result => GetPriority(result.CollectionId))
            //     .Take(Mathf.Max(1, rows))
            //     .ToList();

            cachedSuggestions.Clear();
            orderedSuggestions.Clear();
            foreach (SuggestionResult result in best)
            {
                cachedSuggestions[MakeKey(result.CollectionId, result.Label)] = result;
                orderedSuggestions.Add(result);
            }

            suggestionsRoutine = null;
            SuggestionsReady?.Invoke(new List<SuggestionResult>(orderedSuggestions));

            if (autoSelectFirstWhenReady)
            {
                autoSelectFirstWhenReady = false;
                if (orderedSuggestions.Count > 0) SuggestionAutoSelected?.Invoke(orderedSuggestions[0]);
            }
        }

        public void SelectSuggestion(SuggestionResult suggestion)
        {
            if (string.IsNullOrWhiteSpace(suggestion.Id))
            {
                Debug.LogWarning("[AddressSearchService] Lege resultaat-ID.");
                return;
            }

            targetId = suggestion.DisplayLabel;

            if (string.Equals(suggestion.CollectionId, "adres", StringComparison.OrdinalIgnoreCase))
            {
                StartCoroutine(ResolveSelectedSuggestion(suggestion.Id));
                return;
            }

            if (!suggestion.HasCoordinates)
            {
                Debug.LogWarning("[AddressSearchService] Het geselecteerde resultaat heeft geen coördinaten: " + suggestion.Label);
                return;
            }

            targetCoordinate = suggestion.BoundingBox.Center;
            if (targetCoordinate.PointsLength == 2)
            {
                targetCoordinate = targetCoordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
                targetCoordinate.height = ServiceLocator.GetService<HeightMap>().GetHeight(targetCoordinate);
            }
            
            double minimumDistance = Math.Max(suggestion.BoundingBox.GetSizeMagnitude(), minSizeMagnitudeDistance);
            MoveCameraToLocation(suggestion.BoundingBox.Center, minimumDistance);
        }

        // Compatibility overload for UI code that still passes only ID and label.
        // Prefer SelectSuggestion(SuggestionResult) so the collection and coordinates are retained.
        public void SelectSuggestion(string id, string label)
        {
            SuggestionResult suggestion = orderedSuggestions.FirstOrDefault(result => result.Id == id && result.Label == label);
            if (!string.IsNullOrEmpty(suggestion.Id))
            {
                SelectSuggestion(suggestion);
                return;
            }

            Debug.LogWarning("[AddressSearchService] Resultaat niet meer in de zoekcache gevonden: " + label);
        }
       

        private void MoveCameraToLocation(Coordinate coordinate, double distance)
        {
            targetCoordinate = coordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
            targetCoordinate.height = ServiceLocator.GetService<HeightMap>().GetHeight(targetCoordinate);

            MoveCameraToCoordinate moveCamera = (mainCamera ? mainCamera : Camera.main).GetComponent<MoveCameraToCoordinate>();
            if (moveCamera == null)
            {
                Debug.LogWarning("[AddressSearchService] MoveCameraToCoordinate ontbreekt op de Main Camera.");
                return;
            }

            onCoordinateFound.Invoke(targetCoordinate);
            moveCamera.LookAtTarget(targetCoordinate, distance, true);
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

            string residentialObjectId = ReadString(feature["properties"], "adresseerbaar_object_identificatie");
            if (string.IsNullOrEmpty(residentialObjectId))
            {
                Debug.LogWarning("[AddressSearchService] Geen adresseerbaar object gevonden voor BAG-adres " + id);
                yield break;
            }

            BagDataService bagDataService = ServiceLocator.GetService<BagDataService>();
            List<string> buildingIds = new();
            yield return bagDataService.GetBAGID(residentialObjectId, result => buildingIds = result);

            if (buildingIds == null || buildingIds.Count == 0 || string.IsNullOrEmpty(buildingIds[0]))
            {
                Debug.LogWarning("[AddressSearchService] Geen pand-ID gevonden voor adres " + id);
                yield break;
            }
            
            BoundingBox boundingBox = null;
            yield return bagDataService.GetBuildingBounds(buildingIds[0],  bounds => boundingBox = bounds);

            if (boundingBox == null)
            {
                Debug.LogWarning("[AddressSearchService] Geen bounding box ontvangen voor pand " + buildingIds[0]);
                yield break;
            }

            targetCoordinate = boundingBox.Center;
            if (targetCoordinate.PointsLength == 2)
            {
                targetCoordinate = targetCoordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
                targetCoordinate.height = ServiceLocator.GetService<HeightMap>().GetHeight(targetCoordinate);
            }

            MoveCameraToCoordinate moveCamera = (mainCamera ? mainCamera : Camera.main).GetComponent<MoveCameraToCoordinate>();
            if (moveCamera == null)
            {
                Debug.LogWarning("[AddressSearchService] MoveCameraToCoordinate ontbreekt op de Main Camera.");
                yield break;
            }

            onCoordinateFound.Invoke(targetCoordinate);
            moveCamera.LookAtTarget(targetCoordinate, boundingBox.GetSizeMagnitude(), true);
        }

        private static bool TryReadScore(JSONNode properties, out float score)
        {
            score = 0f;
            string value = ReadString(properties, "score");
            return !string.IsNullOrWhiteSpace(value) && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out score);
        }

        private static string ReadString(JSONNode node, string key)
        {
            if (node == null) return string.Empty;
            JSONNode value = node[key];
            return value == null || value.IsNull ? string.Empty : value.Value;
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

        private void UpdatePointer(Coordinate coordinate)
        {
            if(string.IsNullOrEmpty(targetId)) return;
            
            ShowPointer();
            locationPointer.SetText(targetId);
        }

        private void HidePointer()
        {
            if (locationPointer != null) locationPointer.EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }

        private void ShowPointer()
        {
            if (locationPointer != null) locationPointer.EnableInClassList(UtilityClassConstants.HIDDEN, false);
        }

        private void Update()
        {
            
            
            if (locationElement == null || locationPointer == null || cameraService?.ActiveCamera == null) return;
            UpdateCoordinateTooltip(targetCoordinate, ref locationElement, ref locationPointer);
        }

        private void UpdateCoordinateTooltip(Coordinate coordinate, ref FloatingElement element, ref WorldAnnotation label)
        {
            Vector3 worldPosition = coordinate.ToUnity();
            Vector3 screenPosition = cameraService.ActiveCamera.WorldToScreenPoint(worldPosition);
            Vector2 panelPosition = appRootBehaviour.GetUIPositionFromScreenPosition(screenPosition);
            Vector2 localPosition = worldUIService.FloatingElementsContent.WorldToLocal(panelPosition);
            element.SetPosition(localPosition);

            Vector3 offsetScreenPosition = cameraService.ActiveCamera.WorldToScreenPoint(worldPosition + cameraService.ActiveCamera.transform.right * WorldSpaceOffset);
            float distance = Mathf.Abs(offsetScreenPosition.x - screenPosition.x);
            label.SetLabelOffset(Mathf.Min(distance, MaxPixelPointerOffset));
        }

        private void OnDestroy()
        {
            if (worldUIService != null && locationElement != null) worldUIService.RemoveFromFloatingElementsContent(locationElement);
            if (onCoordinateFound != null) onCoordinateFound.RemoveListener(UpdatePointer);

            ToolService toolService = ServiceLocator.GetService<ToolService>();
            if (toolService != null) toolService.GetTool(ToolType.Search).onClose.RemoveListener(HidePointer);
        }

        private void ClearSuggestions()
        {
            targetId = string.Empty;
            cachedSuggestions.Clear();
            orderedSuggestions.Clear();
            SuggestionsCleared?.Invoke();
        }
    }
}