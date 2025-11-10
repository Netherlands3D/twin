using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Tiles3D
{
    public class MemoryTestGlbLoader : MonoBehaviour
    {
        private const string DefaultCsvResourcePath = "TestData/3dtiles_transforms";

        [Header("Settings")]
        [Tooltip("Maximum aantal GLB bestanden om tegelijk te laden")]
        public int maxConcurrentLoads = 5;
        
        [Tooltip("Parent object waar alle GLB objecten onder komen")]
        public Transform parentTransform;
        
        [Tooltip("Centreer geladen scenes rond het parent object op basis van hun bounding box")]
        public bool centerUsingBounds = true;
        
        [Tooltip("Gebruik positie/rotatie/schaal uit het CSV bestand voor het plaatsen van GLB's")]
        public bool applyCsvTransforms = true;
        
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
        [Tooltip("Hoe lang geladen GLB's zichtbaar blijven voordat ze worden ontladen (seconden)")]
        public float loadPhaseDurationSeconds = 4f;
        [Tooltip("Hoe lang wordt gewacht tussen het ontladen en opnieuw laden (seconden)")]
        public float unloadPhaseDurationSeconds = 4f;

        public string CsvSourceDescription => $"Resources/{DefaultCsvResourcePath}.csv";
        
        private string currentSessionId = "";
        private Coroutine automatedTestRoutine;
        private Coroutine manualLoadCoroutine;
        private bool automatedTestActive;
        private bool clearSceneAfterTestStops;
        
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

            clearSceneAfterTestStops = clearWhenStopped;
            automatedTestActive = false;
        }

        private IEnumerator AutomatedTestRoutine()
        {
            while (automatedTestActive)
            {
                yield return LoadGLBFilesRoutine();

                if (!automatedTestActive)
                {
                    break;
                }

                yield return WaitWhileActive(loadPhaseDurationSeconds);

                if (!automatedTestActive && !clearSceneAfterTestStops)
                {
                    break;
                }

                ClearSceneContents();

                if (!automatedTestActive)
                {
                    break;
                }

                yield return WaitWhileActive(unloadPhaseDurationSeconds);
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
        
        private IEnumerator LoadGLBFilesRoutine()
        {
            EnsureParentTransform();
            isLoading = true;
            loadedFiles = 0;
            
            string[] lines;
            
            TextAsset csvAsset = Resources.Load<TextAsset>(DefaultCsvResourcePath);
            if (csvAsset == null)
            {
                Debug.LogError($"Embedded CSV resource not found at Resources/{DefaultCsvResourcePath}.csv");
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
                yield return Loader.LoadGlb(loadData.url, loadData.position, loadData.rotation, loadData.scale, success =>
                {
                    if (success)
                    {
                        loadedFiles++;
                    }
                });
            }
            
            isLoading = false;
            Debug.Log($"Finished loading {loadedFiles}/{totalFiles} GLB files");
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

            parameters.Remove("key");
            parameters.Remove("session");

            if (!string.IsNullOrEmpty(googleApiKey))
            {
                parameters["key"] = googleApiKey;
            }

            if (!string.IsNullOrEmpty(currentSessionId))
            {
                parameters["session"] = currentSessionId;
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
        
    }
}
