using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace Netherlands3D.Tiles3D
{
    public class MemoryTestGlbLoader : MonoBehaviour
    {
        internal const string TestDataResourceRoot = "TestData";
        internal const string TestDataDirectoryRelativePath = "Packages/eu.netherlands3d.tiles3d/TestData";
        private const string DefaultCsvFileStem = "3dtiles_transforms";

        [Tooltip("Naam van het CSV-bestand in TestData (zonder pad, extensie optioneel).")]
        private string csvFileName = DefaultCsvFileStem;

        public string CsvFileName
        {
            get => csvFileName;
            set => csvFileName = SanitizeCsvFileStem(value);
        }

        [Header("Settings")]
        [Tooltip("Maximum aantal GLB bestanden om tegelijk te laden")]
        public int maxConcurrentLoads = 5;
        
        [Tooltip("Parent object waar alle GLB objecten onder komen")]
        public Transform parentTransform;
        
        [Tooltip("Centreer geladen scenes rond het parent object op basis van hun bounding box")]
        public bool centerUsingBounds = true;
        
        [Tooltip("Gebruik positie/rotatie/schaal uit het CSV bestand voor het plaatsen van GLB's")]
        public bool applyCsvTransforms = true;
        
        [Header("Camera Auto Positioning")]
        [Tooltip("Verplaats camera automatisch naar het midden van de CSV dataset")]
        public bool autoPositionCamera = true;
        [Tooltip("Specifieke camera om te verplaatsen; leeg laat het systeem Camera.main gebruiken")]
        public Transform cameraTransform;
        [Tooltip("Extra hoogte boven het middelpunt")]
        public float cameraHeightOffset = 70f;
        [Tooltip("Hoeveelheid vooruit bewegen ten opzichte van het middelpunt (als fractie)")]
        public float cameraForwardFactor = 0.65f;
        [Tooltip("Minimale afstand die van het middelpunt wordt afgetrokken op de Z-as")]
        public float cameraMinForwardDistance = 450f;
        [Tooltip("Pitch (in graden) voor de auto-gepositioneerde camera")]
        public float cameraPitchDegrees = 15f;
        
        [Header("Google Photorealistic 3D Tiles")]
        [Tooltip("Root tileset url used to request a session id before downloading GLB tiles")]
        public string tilesetRootUrl = "https://tile.googleapis.com:443/v1/3dtiles/root.json";
        
        [SerializeField, HideInInspector]
        private string googleApiKey = "";
        
        [Header("Info")]
        public int totalFiles = 0;
        public int loadedFiles = 0;
        public bool isLoading = false;

        [Header("Automated Test")]
        [Tooltip("Hoe lang wordt gewacht na het ontladen voordat opnieuw wordt geladen (seconden)")]
        [FormerlySerializedAs("unloadPhaseDurationSeconds")]
        public float reloadDelaySeconds = 4f;

        public string CsvSourceDescription => $"Resources/{CsvResourcePath}.csv";
        
        private string currentSessionId = "";
        private Coroutine automatedTestRoutine;
        private Coroutine manualLoadCoroutine;
        private bool automatedTestActive;
        private bool clearSceneAfterTestStops;
        private bool cancelRequested;
        private string CsvResourcePath => $"{TestDataResourceRoot}/{SanitizeCsvFileStem(csvFileName)}";
        
        public bool IsAutomatedTestRunning => automatedTestRoutine != null;
        public bool HasActiveContent => parentTransform != null && parentTransform.childCount > 0;
        private GlbLoader glbLoader;

        internal string GoogleApiKey
        {
            get => googleApiKey;
            set => googleApiKey = value;
        }
        
        [System.Serializable]
        internal class GLBLoadData
        {
            public string url;
            public Vector3 position = Vector3.zero;
            public Quaternion rotation = Quaternion.identity;
            public Vector3 scale = Vector3.one;
        }
        
        void Start()
        {
            EnsureParentTransform();
            ApplyApiKeyFromUrl();
            EnsureCameraReference();
        }

        private void ApplyApiKeyFromUrl()
        {
            string absoluteUrl = Application.absoluteURL;
            if (string.IsNullOrEmpty(absoluteUrl))
            {
                return;
            }

            if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
            {
                return;
            }

            Dictionary<string, string> parameters = ParseQuery(uri.Query);
            if (parameters.TryGetValue("apikey", out string key) && !string.IsNullOrEmpty(key))
            {
                googleApiKey = key;
                Debug.Log("MemoryTestGlbLoader: API key loaded from url parameter.");
            }
        }

        private void EnsureParentTransform()
        {
            if (parentTransform == null)
            {
                parentTransform = transform;
            }
        }

        internal GlbLoader Loader
        {
            get
            {
                glbLoader ??= new GlbLoader(
                    () =>
                    {
                        EnsureParentTransform();
                        return parentTransform;
                    },
                    () => applyCsvTransforms,
                    () => centerUsingBounds,
                    ResolveUrl);
                return glbLoader;
            }
        }
        
        [ContextMenu("Load GLB Files")]
        public void LoadGLBFiles()
        {
            if (IsAutomatedTestRunning)
            {
                Debug.LogWarning("Automated test is running; stop it before manual loading.");
                return;
            }

            if (isLoading)
            {
                Debug.LogWarning("Already loading GLB files");
                return;
            }
            
            if (manualLoadCoroutine != null)
            {
                Debug.LogWarning("Manual load already in progress.");
                return;
            }

            manualLoadCoroutine = StartCoroutine(ManualLoadRoutine());
        }
        
        private IEnumerator ManualLoadRoutine()
        {
            yield return LoadGLBFilesRoutine();
            manualLoadCoroutine = null;
        }

        public void CancelLoading()
        {
            if (!isLoading)
            {
                return;
            }

            ForceStopLoading(clearScene: true);
        }

        public void StartAutomatedTest()
        {
            if (IsAutomatedTestRunning)
            {
                Debug.LogWarning("Automated test is already running.");
                return;
            }

            if (isLoading)
            {
                Debug.LogWarning("Cannot start automated test while loading is in progress.");
                return;
            }

            clearSceneAfterTestStops = false;
            automatedTestActive = true;
            automatedTestRoutine = StartCoroutine(AutomatedTestRoutine());
        }

        public void StopAutomatedTest(bool clearWhenStopped = true)
        {
            if (!IsAutomatedTestRunning)
            {
                if (clearWhenStopped)
                {
                    ClearScene();
                }
                return;
            }

            ForceStopLoading(clearWhenStopped);
        }

        private IEnumerator AutomatedTestRoutine()
        {
            while (automatedTestActive)
            {
                yield return LoadGLBFilesRoutine();

                if (!automatedTestActive && !clearSceneAfterTestStops)
                {
                    break;
                }

                yield return WaitUntilAllTilesLoaded();

                if (!automatedTestActive && !clearSceneAfterTestStops)
                {
                    break;
                }

                ClearSceneContents();

                if (!automatedTestActive)
                {
                    break;
                }

                yield return WaitWhileActive(reloadDelaySeconds);
            }

            if (clearSceneAfterTestStops)
            {
                if (isLoading)
                {
                    while (isLoading)
                    {
                        yield return null;
                    }
                }

                ClearSceneContents();
            }

            automatedTestRoutine = null;
            automatedTestActive = false;
            clearSceneAfterTestStops = false;
        }

        private IEnumerator WaitWhileActive(float duration)
        {
            if (duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration && automatedTestActive)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator WaitUntilAllTilesLoaded()
        {
            if (totalFiles == 0)
            {
                yield break;
            }

            while (automatedTestActive || clearSceneAfterTestStops)
            {
                if (!isLoading && loadedFiles >= totalFiles)
                {
                    yield break;
                }

                yield return null;
            }
        }
        
        private IEnumerator LoadGLBFilesRoutine()
        {
            EnsureParentTransform();
            isLoading = true;
            loadedFiles = 0;
            cancelRequested = false;
            
            string[] lines;
            
            TextAsset csvAsset = Resources.Load<TextAsset>(CsvResourcePath);
            if (csvAsset == null)
            {
                Debug.LogError($"Embedded CSV resource not found at Resources/{CsvResourcePath}.csv");
                isLoading = false;
                yield break;
            }

            List<string> resourceLines = new List<string>();
            using (StringReader reader = new StringReader(csvAsset.text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        resourceLines.Add(line);
                    }
                }
            }

            lines = resourceLines.ToArray();
            
            List<GLBLoadData> loadDataList = new List<GLBLoadData>();
            
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                
                string[] parts = line.Split(',');
                if (parts.Length >= 11)
                {
                    GLBLoadData loadData = new GLBLoadData();
                    loadData.url = parts[0];

                    float posX = 0f, posY = 0f, posZ = 0f;
                    float rotX = 0f, rotY = 0f, rotZ = 0f, rotW = 1f;
                    float scaleX = 1f, scaleY = 1f, scaleZ = 1f;

                    // Parse position
                    if (TryParseFloat(parts[1], out posX) &&
                        TryParseFloat(parts[2], out posY) &&
                        TryParseFloat(parts[3], out posZ))
                    {
                        loadData.position = new Vector3(posX, posY, posZ);
                    }
                    
                    // Parse rotation
                    if (TryParseFloat(parts[4], out rotX) &&
                        TryParseFloat(parts[5], out rotY) &&
                        TryParseFloat(parts[6], out rotZ) &&
                        TryParseFloat(parts[7], out rotW))
                    {
                        float rotationSqrMagnitude = rotX * rotX + rotY * rotY + rotZ * rotZ + rotW * rotW;
                        if (rotationSqrMagnitude > Mathf.Epsilon)
                        {
                            Quaternion parsedRotation = new Quaternion(rotX, rotY, rotZ, rotW);
                            parsedRotation.Normalize();
                            loadData.rotation = parsedRotation;
                        }
                    }
                    else
                    {
                        loadData.rotation = Quaternion.identity;
                    }
                    
                    // Parse scale
                    if (TryParseFloat(parts[8], out scaleX) &&
                        TryParseFloat(parts[9], out scaleY) &&
                        TryParseFloat(parts[10], out scaleZ))
                    {
                        loadData.scale = new Vector3(scaleX, scaleY, scaleZ);
                    }
                    else
                    {
                        loadData.scale = Vector3.one;
                    }
                    
                    loadDataList.Add(loadData);
                }
            }
            
            totalFiles = loadDataList.Count;
            Debug.Log($"Found {totalFiles} GLB files to load");
            
            if (totalFiles > 0)
            {
                var averagePosition = CalculateAveragePosition(loadDataList);
                if (averagePosition.HasValue)
                {
                    AutoPositionCamera(averagePosition.Value);
                }
            }
            
            if (totalFiles == 0)
            {
                isLoading = false;
                yield break;
            }
            
            currentSessionId = "";
            if (!string.IsNullOrEmpty(googleApiKey))
            {
                yield return FetchSessionId();
            }
            
            foreach (var loadData in loadDataList)
            {
                if (cancelRequested)
                {
                    break;
                }

                yield return Loader.LoadGlb(loadData.url, loadData.position, loadData.rotation, loadData.scale, success =>
                {
                    if (success)
                    {
                        loadedFiles++;
                    }
                });
            }
            
            isLoading = false;

            if (cancelRequested)
            {
                Debug.Log("Loading cancelled by user.");
                ClearSceneContents();
                cancelRequested = false;
            }
            else
            {
                Debug.Log($"Finished loading {loadedFiles}/{totalFiles} GLB files");
            }
        }

        private void ForceStopLoading(bool clearScene)
        {
            cancelRequested = false;

            if (manualLoadCoroutine != null)
            {
                StopCoroutine(manualLoadCoroutine);
                manualLoadCoroutine = null;
            }

            if (automatedTestRoutine != null)
            {
                StopCoroutine(automatedTestRoutine);
                automatedTestRoutine = null;
            }

            automatedTestActive = false;
            clearSceneAfterTestStops = false;
            isLoading = false;

            if (clearScene)
            {
                ClearSceneContents();
            }
        }
        

        [ContextMenu("Clear Scene")]
        public void ClearScene()
        {
            if (isLoading)
            {
                Debug.LogWarning("Kan de scene niet leegmaken terwijl GLB's worden geladen.");
                return;
            }

            ClearSceneContents();
            Debug.Log("Scene cleared");
        }

        internal void ClearSceneContents()
        {
            Loader.UnloadGlb();
            loadedFiles = 0;
            totalFiles = 0;
        }

        private void EnsureCameraReference()
        {
            if (!autoPositionCamera || cameraTransform != null)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
        }

        private IEnumerator FetchSessionId()
        {
            if (string.IsNullOrEmpty(tilesetRootUrl))
            {
                Debug.LogWarning("Tileset root url is empty, skipping session request.");
                yield break;
            }

            if (string.IsNullOrEmpty(googleApiKey))
            {
                Debug.LogWarning("Google API key is empty, skipping session request.");
                yield break;
            }

            string requestUrl = AppendOrReplaceQuery(tilesetRootUrl, new Dictionary<string, string>
            {
                { "key", googleApiKey }
            });

            using (UnityWebRequest request = UnityWebRequest.Get(requestUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to fetch tileset root: {request.error}");
                    yield break;
                }

                string responseText = request.downloadHandler.text;
                string newSession = ExtractSessionIdFromTilesetJson(responseText);

                if (string.IsNullOrEmpty(newSession))
                {
                    Debug.LogWarning("Tileset response did not contain a session id in its content.");
                    yield break;
                }

                currentSessionId = newSession;
                Debug.Log("Obtained new tileset session id.");
            }
        }

        private string ResolveUrl(string originalUrl)
        {
            if (string.IsNullOrEmpty(originalUrl))
            {
                return originalUrl;
            }

            if (!originalUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return originalUrl;
            }

            UriBuilder builder;
            try
            {
                builder = new UriBuilder(originalUrl);
            }
            catch (UriFormatException)
            {
                return originalUrl;
            }

            Dictionary<string, string> parameters = ParseQuery(builder.Query);

            bool isGoogleTilesUrl = builder.Host.IndexOf("tile.googleapis.com", StringComparison.OrdinalIgnoreCase) >= 0;
            bool hasKey = !string.IsNullOrEmpty(googleApiKey);
            bool hasSession = !string.IsNullOrEmpty(currentSessionId);

            if (isGoogleTilesUrl)
            {
                if (hasKey)
                {
                    parameters["key"] = googleApiKey;
                }
                else
                {
                    parameters.Remove("key");
                }

                if (hasSession)
                {
                    parameters["session"] = currentSessionId;
                }
                else
                {
                    parameters.Remove("session");
                }
            }
            else
            {
                parameters.Remove("key");
                parameters.Remove("session");
            }

            builder.Query = BuildQueryString(parameters);
            return builder.Uri.ToString();
        }

        private string AppendOrReplaceQuery(string url, Dictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            UriBuilder builder;
            try
            {
                builder = new UriBuilder(url);
            }
            catch (UriFormatException)
            {
                return url;
            }

            Dictionary<string, string> existingParams = ParseQuery(builder.Query);

            foreach (var kvp in parameters)
            {
                existingParams.Remove(kvp.Key);
            }

            foreach (var kvp in parameters)
            {
                if (kvp.Value == null)
                {
                    continue;
                }

                existingParams[kvp.Key] = kvp.Value;
            }

            builder.Query = BuildQueryString(existingParams);
            return builder.Uri.ToString();
        }

        private Vector3? CalculateAveragePosition(List<GLBLoadData> loadDataList)
        {
            if (loadDataList == null || loadDataList.Count == 0)
            {
                return null;
            }

            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var data in loadDataList)
            {
                sum += data.position;
                count++;
            }

            if (count == 0)
            {
                return null;
            }

            return sum / count;
        }

        private void AutoPositionCamera(Vector3 focusPoint)
        {
            if (!autoPositionCamera)
            {
                return;
            }

            Transform targetCamera = cameraTransform;
            if (targetCamera == null)
            {
                EnsureCameraReference();
                targetCamera = cameraTransform;
            }

            if (targetCamera == null)
            {
                Debug.LogWarning("MemoryTestGlbLoader: Geen camera gevonden om te positioneren.");
                return;
            }

            float forwardOffset = Mathf.Max(cameraMinForwardDistance, Mathf.Abs(focusPoint.z) * cameraForwardFactor);
            Vector3 cameraPosition = new Vector3(
                focusPoint.x,
                focusPoint.y + cameraHeightOffset,
                focusPoint.z - forwardOffset);

            targetCamera.position = cameraPosition;
            targetCamera.rotation = Quaternion.Euler(cameraPitchDegrees, 0f, 0f);
        }

        private Dictionary<string, string> ParseQuery(string query)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(query))
            {
                return result;
            }

            string trimmed = query;
            if (trimmed.StartsWith("?"))
            {
                trimmed = trimmed.Substring(1);
            }

            string[] pairs = trimmed.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                string[] parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
                {
                    continue;
                }

                string key = Uri.UnescapeDataString(parts[0]);
                string value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";

                result[key] = value;
            }

            return result;
        }

        private string BuildQueryString(Dictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (var kvp in parameters)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                {
                    continue;
                }

                if (!first)
                {
                    sb.Append('&');
                }

                sb.Append(Uri.EscapeDataString(kvp.Key));

                if (kvp.Value != null)
                {
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(kvp.Value));
                }

                first = false;
            }

            return sb.ToString();
        }

        private string ExtractSessionIdFromTilesetJson(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText))
            {
                return "";
            }

            Match match = Regex.Match(jsonText, @"\?session=([A-Za-z0-9_\-]+)", RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return "";
        }

        private bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(
                value,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out result);
        }

        internal static string SanitizeCsvFileStem(string raw)
        {
            string trimmed = raw?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return DefaultCsvFileStem;
            }

            trimmed = trimmed.Replace('\\', '/');
            int slashIndex = trimmed.LastIndexOf('/');
            if (slashIndex >= 0 && slashIndex < trimmed.Length - 1)
            {
                trimmed = trimmed.Substring(slashIndex + 1);
            }

            if (trimmed.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 4);
            }

            if (string.IsNullOrEmpty(trimmed))
            {
                return DefaultCsvFileStem;
            }

            return trimmed;
        }

#if UNITY_EDITOR
        internal static string GetAbsoluteTestDataDirectory()
        {
            string assetsPath = Application.dataPath;
            if (string.IsNullOrEmpty(assetsPath))
            {
                return null;
            }

            string projectRoot = Directory.GetParent(assetsPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                return null;
            }

            return Path.GetFullPath(Path.Combine(projectRoot, TestDataDirectoryRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
#endif
        
    }
}
