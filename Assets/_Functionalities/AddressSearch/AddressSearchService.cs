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
        private const string locationApiBaseUrl = "https://api.pdok.nl/kadaster/location-api/v1";
        private const int rows =50;
        private const int maxResults = 10;
        private const float minimumScore = 0;
        private const int charactersNeededBeforeSearch = 2;

        [Header("BAG API")]
        [SerializeField] private const string bagApiBaseUrl = "https://api.pdok.nl/kadaster/bag/ogc/v2";
      
        [SerializeField] private Camera mainCamera;
        private const double minSizeMagnitudeDistance = 20; //inrichtingselementen have no bbox size magnitudes

        public UnityEvent<Coordinate> onCoordinateFound = new();
        public event Action<List<SuggestionResult>> SuggestionsReady;
        public event Action SuggestionsCleared;
        public event Action<SuggestionResult> SuggestionAutoSelected;
        
        public string SelectedLocationLabel => selectedLocationLabel;
        public Coordinate SelectedCoordinate => selectedCoordinate;
        
        private string selectedLocationLabel;
        private Coordinate selectedCoordinate;
        private Coroutine resolveSelectedSuggestionCoroutine;

        // Volgorde waarin collecties in de lijst verschijnen (eerst grote gebieden en adressen).
        private static readonly Dictionary<string, string> CollectionPriorityOrder = new Dictionary<string, string>
            {
                { "plaats", "1" },
                { "gemeentegebied", "1" },
                { "woonplaats", "1" },
                { "provinciegebied", "1" },
                { "geografisch_gebied", "0.1" },
                { "functioneel_gebied", "0.1" },
                { "perceel", "0.1" },
                { "gebouw", "0.5" },
                { "wegdeel", "0.1" },
                { "waterdeel", "0.1" },
                { "spoorbaandeel", "0.1" },
                { "inrichtingselement", "0.1" },
                { "adres", "0.5" }
            };

        // Alle PDOK Location API data is CRS84 (lon, lat), dat is gelijk aan WGS84 maar dan met de volgorde lon/lat.
        private const string Crs84 = "http://www.opengis.net/def/crs/OGC/1.3/CRS84";
       
        // Dezelfde resultaten in weergavevolgorde voor de UI.
        private readonly List<SuggestionResult> orderedSuggestions = new();

        private Coroutine suggestionsRoutine;
        private bool autoSelectFirstWhenReady;
        private int searchRequestVersion;
        
        
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

        public static string GetCollectionDisplayName(string collectionId) => CollectionDisplayNames.TryGetValue(collectionId, out var name) ? name : collectionId;

        private static float GetCollectionRelevance(string collectionId)
        {
            if (!CollectionPriorityOrder.TryGetValue(collectionId, out string relevance))
                return 0f;

            if (float.TryParse(relevance, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                return value;

            return 0f;
        }
        
        private static string GetNetherlandsBbox()
        {
            BoundingBox bounds = StandardBoundingBoxes.Wgs84LatLon_NetherlandsBounds;

            Coordinate min = bounds.BottomLeft.Convert(CoordinateSystem.CRS84);
            Coordinate max = bounds.TopRight.Convert(CoordinateSystem.CRS84);

            return $"{min.easting.ToString(CultureInfo.InvariantCulture)}," +
                   $"{min.northing.ToString(CultureInfo.InvariantCulture)}," +
                   $"{max.easting.ToString(CultureInfo.InvariantCulture)}," +
                   $"{max.northing.ToString(CultureInfo.InvariantCulture)}";
        }

        private void Start()
        {
            if (!mainCamera) mainCamera = Camera.main;
            
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

            if (string.IsNullOrWhiteSpace(textInput) || (!autoSelectFirst && textInput.Trim().Length < charactersNeededBeforeSearch))
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

            var results = new List<SuggestionResult>();

            // Build one request containing all collections and their relevance weights.
            string url = locationApiBaseUrl.TrimEnd('/') + "/search?q=" + UnityWebRequest.EscapeURL(query);

            foreach (string collectionId in CollectionPriorityOrder.Keys)
            {
                float relevance = GetCollectionRelevance(collectionId);

                url += "&" + UnityWebRequest.EscapeURL(
                    $"{collectionId}[version]") + "=1";

                url += "&" + UnityWebRequest.EscapeURL(
                           $"{collectionId}[relevance]") + "=" +
                       relevance.ToString(CultureInfo.InvariantCulture);
            }

            url += "&limit=" + Mathf.Clamp(rows, 1, 50);
            url += "&crs=" + UnityWebRequest.EscapeURL(Crs84);
            url += "&f=json";
            url += "&bbox=" + GetNetherlandsBbox();

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (requestVersion != searchRequestVersion) yield break;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[AddressSearchService] Zoekopdracht mislukt: " + request.error);
                suggestionsRoutine = null;
                yield break;
            }

            JSONNode root;

            try
            {
                root = JSON.Parse(request.downloadHandler.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AddressSearchService] Ongeldige JSON: " + exception.Message);
                suggestionsRoutine = null;
                yield break;
            }

            JSONNode features = root["features"];

            if (features != null && features.IsArray)
            {
                for (int i = 0; i < features.Count; i++)
                {
                    JSONNode feature = features[i];
                    JSONNode properties = feature["properties"];

                    string collectionId = ReadString(properties, "collection_id");

                    if (string.IsNullOrWhiteSpace(collectionId)) continue;

                    string id = ReadString(feature, "id");
                    if (string.IsNullOrWhiteSpace(id)) id = ReadString(properties, "id");
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    // Reject results without a score and every result below the configured threshold.
                    if (!TryReadScore(properties, out float score) || score < minimumScore) continue;

                    string label = ReadString(properties, "display_name");
                    if (string.IsNullOrWhiteSpace(label)) label = ReadString(properties, "weergavenaam");
                    if (string.IsNullOrWhiteSpace(label)) label = ReadString(properties, "name");
                    if (string.IsNullOrWhiteSpace(label)) label = id;

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

                        bbox = new BoundingBox(
                            new Coordinate(CoordinateSystem.WGS84_LatLon, minLat, minLon),
                            new Coordinate(CoordinateSystem.WGS84_LatLon, maxLat, maxLon)
                        );

                        hasBounds = true;
                        hasCoordinates = true;
                    }

                    JSONNode geometry = feature["geometry"];
                    JSONNode coords = geometry != null ? geometry["coordinates"] : null;

                    if (geometry != null && string.Equals(ReadString(geometry, "type"), "Point", StringComparison.OrdinalIgnoreCase) &&
                        coords != null && coords.IsArray && coords.Count >= 2)
                    {
                        double lon = coords[0].AsDouble;
                        double lat = coords[1].AsDouble;

                        var point = new Coordinate(CoordinateSystem.WGS84_LatLon, lat, lon);

                        if (!hasBounds)
                            bbox = new BoundingBox(point, point);

                        hasCoordinates = true;
                    }

                    var result = new SuggestionResult(
                        id,
                        label,
                        GetCollectionDisplayName(collectionId),
                        collectionId,
                        score,
                        bbox,
                        hasCoordinates
                    );

                    results.Add(result);
                }
            }

            if (requestVersion != searchRequestVersion) yield break;

            List<SuggestionResult> best = results.Take(maxResults).ToList();

            orderedSuggestions.Clear();

            foreach (SuggestionResult result in best)
            {
                orderedSuggestions.Add(result);
            }

            suggestionsRoutine = null;
            SuggestionsReady?.Invoke(new List<SuggestionResult>(orderedSuggestions));

            if (autoSelectFirstWhenReady)
            {
                autoSelectFirstWhenReady = false;

                if (orderedSuggestions.Count > 0)
                    SuggestionAutoSelected?.Invoke(orderedSuggestions[0]);
            }
        }
        
        public void SelectSuggestion(SuggestionResult suggestion)
        {
            if (string.IsNullOrWhiteSpace(suggestion.Id))
            {
                Debug.LogWarning("[AddressSearchService] Lege resultaat-ID.");
                return;
            }

            selectedLocationLabel = suggestion.DisplayLabel;
            if(resolveSelectedSuggestionCoroutine != null) 
                StopCoroutine(resolveSelectedSuggestionCoroutine);

            if (string.Equals(suggestion.CollectionId, "adres", StringComparison.OrdinalIgnoreCase))
            {
                resolveSelectedSuggestionCoroutine = StartCoroutine(ResolveSelectedSuggestion(suggestion));
                return;
            }

            if (!suggestion.HasCoordinates)
            {
                Debug.LogWarning("[AddressSearchService] Het geselecteerde resultaat heeft geen coördinaten: " + suggestion.Label);
                return;
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
       
         private IEnumerator ResolveSelectedSuggestion(SuggestionResult suggestion)
        {
            string url = bagApiBaseUrl.TrimEnd('/') + "/collections/adres/items/" + UnityWebRequest.EscapeURL(suggestion.Id) + "?f=json";
            JSONNode feature;
            
            //before trying to receive all results we could move to the location until its ready
            MoveCameraToLocation(suggestion.BoundingBox.Center, minSizeMagnitudeDistance);

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
                Debug.LogWarning("[AddressSearchService] Geen adresseerbaar object gevonden voor BAG-adres " + suggestion.Id);
                yield break;
            }

            BagDataService bagDataService = ServiceLocator.GetService<BagDataService>();
            List<string> buildingIds = new();
            yield return bagDataService.GetBAGID(residentialObjectId, result => buildingIds = result);

            if (buildingIds == null || buildingIds.Count == 0 || string.IsNullOrEmpty(buildingIds[0]))
            {
                Debug.LogWarning("[AddressSearchService] Geen pand-ID gevonden voor adres " + suggestion.Id);
                yield break;
            }
            
            BoundingBox boundingBox = null;
            yield return bagDataService.GetBuildingBounds(buildingIds[0],  bounds => boundingBox = bounds);

            if (boundingBox == null)
            {
                Debug.LogWarning("[AddressSearchService] Geen bounding box ontvangen voor pand " + buildingIds[0]);
                MoveCameraToLocation(suggestion.BoundingBox.Center, minSizeMagnitudeDistance);
                yield break;
            }
            MoveCameraToLocation(boundingBox.Center, boundingBox.GetSizeMagnitude());
            onCoordinateFound.Invoke(selectedCoordinate);
        }

        private void MoveCameraToLocation(Coordinate coordinate, double targetSize)
        {
            selectedCoordinate = coordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
            selectedCoordinate.height = ServiceLocator.GetService<HeightMap>().GetHeight(selectedCoordinate);

            MoveCameraToCoordinate moveCamera = (mainCamera ? mainCamera : Camera.main).GetComponent<MoveCameraToCoordinate>();
            if (moveCamera == null)
            {
                Debug.LogWarning("[AddressSearchService] MoveCameraToCoordinate ontbreekt op de Main Camera.");
                return;
            }

            onCoordinateFound.Invoke(selectedCoordinate);
            moveCamera.LookAtTarget(selectedCoordinate, targetSize, true);
            
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
        
        private void ClearSuggestions()
        {
            selectedLocationLabel = string.Empty;
            orderedSuggestions.Clear();
            SuggestionsCleared?.Invoke();
        }
    }
}