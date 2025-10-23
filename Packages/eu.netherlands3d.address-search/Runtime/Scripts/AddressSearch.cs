using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Netherlands3D.Coordinates;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.AddressSearch
{
    [RequireComponent(typeof(TMP_InputField))]
    public class AddressSearch : MonoBehaviour
    {
        private TMP_InputField searchInputField;

        [Tooltip("The WFS endpoint for retrieving BAG information, see: https://www.pdok.nl/geo-services/-/article/basisregistratie-adressen-en-gebouwen-ba-1")]
        [SerializeField] private string bagWfsEndpoint = "https://service.pdok.nl/lv/bag/wfs/v2_0";

        [Tooltip("The endpoint for retrieving suggestions when looking up addresses, see: https://www.pdok.nl/restful-api/-/article/pdok-locatieserver-1")]
        [SerializeField] private string locationSuggestionEndpoint = "https://api.pdok.nl/bzk/locatieserver/search/v3_1/suggest";

        [Tooltip("The endpoint for looking up addresses, see: https://www.pdok.nl/restful-api/-/article/pdok-locatieserver-1")]
        [SerializeField] private string locationLookupEndpoint = "https://api.pdok.nl/bzk/locatieserver/search/v3_1/lookup";

        [SerializeField] private string searchWithinCity = "Amsterdam";

        [Tooltip("The type of address to filter on, see: https://www.pdok.nl/restful-api/-/article/pdok-locatieserver-1")]
        [SerializeField] private string typeFilter = "";
        [SerializeField] private int rows = 5;
        [SerializeField] private int charactersNeededBeforeSearch = 2;
        [SerializeField] private GameObject resultsParent;
        [SerializeField] private Button suggestionPrefab;

        [Header("Camera Controls")]
        [SerializeField] private bool moveCamera = true;
        [SerializeField] private bool easeCamera = false;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Quaternion targetCameraRotation = Quaternion.Euler(45, 0, 0);
        [SerializeField] public AnimationCurve cameraMoveCurve;

        [Header("Keyboard Navigation")]
        [SerializeField] private Color selectionColor = new Color(0.85f, 0.90f, 1f, 1f); // soft highlight
        [SerializeField] private Color unselectedColor = Color.white;

        public UnityEvent<bool> onClearButtonToggled = new();
        public UnityEvent<Coordinate> onCoordinateFound = new();
        public UnityEvent<List<string>> onSelectedBuildings = new();

        public string SearchInput { get => searchInputField.text; set => searchInputField.text = Regex.Replace(value, "<.*?>", string.Empty); }

        private const string REPLACEMENT_STRING = "{SEARCHTERM}";

        public bool IsFocused => searchInputField.isFocused;

        // Enter-to-autoselect behavior
        private bool autoSelectFirstWhenReady = false;
        private Coroutine suggestionsRoutine;
        private string lastSubmittedTerm = "";

        // Keyboard navigation state
        private readonly List<Button> resultButtons = new();
        private readonly List<Image> resultImages = new();  // targetGraphic or added Image
        private int selectedIndex = -1;
        private bool navigatingResults = false;

        private void Start()
        {
            if (!mainCamera) mainCamera = Camera.main;

            searchInputField = GetComponent<TMP_InputField>();
            searchInputField.onValueChanged.AddListener(delegate { GetSuggestions(searchInputField.text); });

            // Enter/Return submits
            searchInputField.onSubmit.AddListener(OnSubmitSearch);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return; // no keyboard available (e.g. mobile)

            if (IsFocused && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                OnSubmitSearch(searchInputField.text);
            }

            // Only handle arrow navigation when we actually have results
            if (resultsParent.gameObject.activeSelf && resultButtons.Count > 0 && IsFocused)
            {
                if (keyboard.downArrowKey.wasPressedThisFrame)
                {
                    navigatingResults = true;
                    MoveSelection(1);
                }
                else if (keyboard.upArrowKey.wasPressedThisFrame)
                {
                    navigatingResults = true;
                    MoveSelection(-1);
                }
            }
        }

        public void ClearInput()
        {
            ClearSearchResults();
            searchInputField.text = "";
            GetSuggestions(searchInputField.text);
            searchInputField.Select();
        }

        public void GetSuggestions(string textInput = "")
        {
            // Only stop the running suggestions coroutine (don’t kill camera lerps etc.)
            if (suggestionsRoutine != null) StopCoroutine(suggestionsRoutine);

            var isEmpty = textInput.Trim() == "";
            if (isEmpty)
            {
                ClearSearchResults();
                onClearButtonToggled.Invoke(false);
                return;
            }

            if (textInput.Length <= charactersNeededBeforeSearch)
            {
                ClearSearchResults();
                return;
            }

            onClearButtonToggled.Invoke(true);

            suggestionsRoutine = StartCoroutine(FindSearchSuggestions(textInput));
        }

        private void OnSubmitSearch(string text)
        {
            // If a specific selection is active, use it
            if (resultsParent.transform.childCount > 0)
            {
                int indexToUse = selectedIndex >= 0 && selectedIndex < resultButtons.Count ? selectedIndex : 0;
                Button chosen = resultButtons[indexToUse];
                if (chosen != null) chosen.onClick.Invoke();
                return;
            }

            // Otherwise: request suggestions and auto-pick the first when ready
            autoSelectFirstWhenReady = true;
            lastSubmittedTerm = text;

            // Fetch directly (bypassing threshold/empty checks)
            if (suggestionsRoutine != null) StopCoroutine(suggestionsRoutine);
            suggestionsRoutine = StartCoroutine(FindSearchSuggestions(text));
        }

        IEnumerator FindSearchSuggestions(string searchTerm)
        {
            string urlEncodedSearchTerm = UnityWebRequest.EscapeURL(searchTerm);

            var searchWithinQuery = (searchWithinCity.Length > 0) ? $"and%20{searchWithinCity}%20" : "";
            var filterTypeQuery = (typeFilter.Length > 0) ? $"and%20type:{typeFilter}" : "";
            string url = $"{locationSuggestionEndpoint}?q={urlEncodedSearchTerm}%20{searchWithinQuery}{filterTypeQuery}&rows={rows}".Replace(REPLACEMENT_STRING, urlEncodedSearchTerm);

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Geen verbinding");
                yield break;
            }

            ClearSearchResults();

            var jsonNode = JSON.Parse(webRequest.downloadHandler.text);
            var results = jsonNode["response"]["docs"];

            for (int i = 0; i < results.Count; i++)
            {
                var ID = results[i]["id"];
                var label = results[i]["weergavenaam"];
                GenerateResultItem(ID, label);
            }

            if (results.Count > 0)
            {
                resultsParent.gameObject.SetActive(true);

                // Default selection (top) when results open
                selectedIndex = 0;
                navigatingResults = false; // will become true when user hits arrow keys
                UpdateSelectionVisuals();
            }

            // If user pressed Enter before results loaded → auto-pick
            if (autoSelectFirstWhenReady && resultButtons.Count > 0)
            {
                autoSelectFirstWhenReady = false; // reset flag
                int indexToUse = selectedIndex >= 0 && selectedIndex < resultButtons.Count ? selectedIndex : 0;
                Button firstButton = resultButtons[indexToUse];
                if (firstButton != null) firstButton.onClick.Invoke();
            }
        }

        private void ClearSearchResults()
        {
            foreach (Transform child in resultsParent.transform)
                Destroy(child.gameObject);

            resultsParent.gameObject.SetActive(false);

            resultButtons.Clear();
            resultImages.Clear();
            ResetNavigation();
        }

        private void ResetNavigation()
        {
            selectedIndex = -1;
            navigatingResults = false;
        }

        private void MoveSelection(int delta)
        {
            if (resultButtons.Count == 0) return;

            if (selectedIndex == -1)
            {
                selectedIndex = (delta > 0) ? 0 : resultButtons.Count - 1;
            }
            else
            {
                selectedIndex = (selectedIndex + delta + resultButtons.Count) % resultButtons.Count;
            }

            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            for (int i = 0; i < resultButtons.Count; i++)
            {
                var img = resultImages[i];
                if (img != null)
                {
                    img.color = (i == selectedIndex) ? selectionColor : unselectedColor;
                }
            }
        }

        private void GenerateResultItem(string ID, string label)
        {
            Button buttonObject = (suggestionPrefab != null) ? Instantiate(suggestionPrefab) : GenerateResultButton(label);
            buttonObject.transform.SetParent(resultsParent.transform, false);

            TextMeshProUGUI textObject = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textObject == null)
            {
                Debug.Log("Make sure your suggestionPrefab has a TextMeshProUGUI component");
                return;
            }
            textObject.text = label;

            // Ensure we have an Image to tint for selection visuals
            Image img = null;
            if (buttonObject.targetGraphic is Image tgImg)
            {
                img = tgImg;
            }
            else
            {
                img = buttonObject.GetComponent<Image>();
                if (img == null) img = buttonObject.gameObject.AddComponent<Image>();
                img.color = unselectedColor;
            }

            // Keep references for keyboard navigation
            resultButtons.Add(buttonObject);
            resultImages.Add(img);

            // Click handler (mouse or simulated by keyboard/enter)
            buttonObject.onClick.AddListener(delegate { GeoDataLookup(ID, label); });

            // Optional: hovering with mouse updates selection visuals (nice UX)
            var trigger = buttonObject.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = buttonObject.gameObject.AddComponent<EventTrigger>();

            AddOrUpdateTrigger(trigger, EventTriggerType.PointerEnter, (_) =>
            {
                int idx = resultButtons.IndexOf(buttonObject);
                if (idx >= 0)
                {
                    selectedIndex = idx;
                    UpdateSelectionVisuals();
                }
            });
        }

        private void AddOrUpdateTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(action));
            trigger.triggers.Add(entry);
        }

        private Button GenerateResultButton(string label)
        {
            GameObject suggestion = new GameObject { name = label };

            RectTransform rt = suggestion.AddComponent<RectTransform>();
            rt.SetParent(resultsParent.transform);
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(160, 32);

            suggestion.SetActive(true);

            // Background for selection color
            Image bg = suggestion.AddComponent<Image>();
            bg.color = unselectedColor;

            // Text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(suggestion.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.offsetMin = new Vector2(10, 4);
            textRT.offsetMax = new Vector2(-10, -4);

            TextMeshProUGUI textObject = textGO.AddComponent<TextMeshProUGUI>();
            textObject.color = Color.black;
            textObject.fontSize = 18;
            textObject.text = label;
            textObject.enableAutoSizing = true;
            textObject.alignment = TextAlignmentOptions.MidlineLeft;

            // Button
            Button buttonObject = suggestion.AddComponent<Button>();
            buttonObject.targetGraphic = bg;

            return buttonObject;
        }

        private void GeoDataLookup(string ID, string label)
        {
            searchInputField.SetTextWithoutNotify(label);
            StartCoroutine(GeoDataLookupRoutine(ID));
            ClearSearchResults();
        }

        IEnumerator GeoDataLookupRoutine(string ID)
        {
            string url = $"{locationLookupEndpoint}?id={ID}";
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Geen verbinding");
                yield break;
            }

            var jsonNode = JSON.Parse(webRequest.downloadHandler.text);
            var results = jsonNode["response"]["docs"];

            string centroid = results[0]["centroide_ll"];
            Debug.Log($"Centroid: {centroid}");
            string residentialObjectID = results[0]["adresseerbaarobject_id"];

            Vector3 targetLocation = ExtractUnityLocation(ref centroid);

            onCoordinateFound.Invoke(new Coordinate(targetLocation));

            if (moveCamera)
            {
                var targetPos = new Vector3(targetLocation.x, 300, targetLocation.z - 300);

                if (easeCamera)
                {
                    StartCoroutine(LerpCamera(mainCamera.gameObject, targetPos, targetCameraRotation, 2));
                    yield return new WaitForSeconds(2);
                    StartCoroutine(GetBAGID(residentialObjectID));
                    yield break;
                }

                mainCamera.gameObject.transform.position = targetPos;
            }
        }

        private static Vector3 ExtractUnityLocation(ref string locationData)
        {
            locationData = locationData.Replace("POINT(", "").Replace(")", "").Replace("\"", "");
            string[] lonLat = locationData.Split(' ');

            double.TryParse(lonLat[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon);
            double.TryParse(lonLat[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat);

            var wgs84 = new Coordinate(CoordinateSystem.WGS84_LatLon, lat, lon);
            var unityCoordinate = wgs84.ToUnity();

            return unityCoordinate;
        }

        IEnumerator LerpCamera(GameObject targetObj, Vector3 endPos, Quaternion endRot, float duration)
        {
            float t = 0;
            Vector3 startPos = targetObj.transform.position;
            Quaternion startRot = targetObj.transform.rotation;
            while (t < duration)
            {
                targetObj.transform.position = Vector3.Lerp(startPos, endPos, cameraMoveCurve.Evaluate(t / duration));
                targetObj.transform.rotation = Quaternion.Lerp(startRot, endRot, cameraMoveCurve.Evaluate(t / duration));
                t += Time.deltaTime;
                yield return null;
            }

            targetObj.transform.position = endPos;
        }

        IEnumerator GetBAGID(string residentialObjectID)
        {
            string url = $"{bagWfsEndpoint}?SERVICE=WFS&VERSION=2.0.0&outputFormat=geojson&REQUEST=GetFeature&typeName=bag:verblijfsobject&count=100&outputFormat=xml&srsName=EPSG:28992&filter=<Filter><PropertyIsEqualTo><PropertyName>identificatie</PropertyName><Literal>{residentialObjectID}</Literal></PropertyIsEqualTo></Filter>";

            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Geen verbinding");
                yield break;
            }

            JSONNode jsonNode = JSON.Parse(webRequest.downloadHandler.text);
            JSONNode bagId = jsonNode["features"][0]["properties"]["pandidentificatie"];

#if UNITY_EDITOR
            Debug.Log($"BAG ID: {bagId}");
#endif

            List<string> bagIDs = new List<string> { bagId };

            onSelectedBuildings.Invoke(bagIDs);
        }
    }
}
