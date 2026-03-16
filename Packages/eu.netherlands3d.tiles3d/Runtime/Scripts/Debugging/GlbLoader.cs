using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Tiles3D
{
    /// <summary>
    /// Loads and unloads GLB files against a supplied parent transform.
    /// Designed so higher level systems can inject configuration per load.
    /// </summary>
    internal class GlbLoader
    {
        private readonly Func<Transform> parentProvider;
        private readonly Func<bool> applyTransformProvider;
        private readonly Func<bool> centerUsingBoundsProvider;
        private readonly Func<string, string> resolveUrl;
        private readonly List<GltfImport> activeImports = new List<GltfImport>();

        internal GlbLoader(
            Func<Transform> parentProvider,
            Func<bool> applyTransformProvider,
            Func<bool> centerUsingBoundsProvider,
            Func<string, string> resolveUrl)
        {
            this.parentProvider = parentProvider ?? throw new ArgumentNullException(nameof(parentProvider));
            this.applyTransformProvider = applyTransformProvider ?? throw new ArgumentNullException(nameof(applyTransformProvider));
            this.centerUsingBoundsProvider = centerUsingBoundsProvider ?? throw new ArgumentNullException(nameof(centerUsingBoundsProvider));
            this.resolveUrl = resolveUrl ?? (original => original);
        }

        internal IEnumerator LoadGlb(
            string url,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Action<bool> onCompleted = null)
        {
            bool success = false;
            GameObject glbRoot = null;
            GltfImport gltf = null;

            try
            {
                Transform parent = parentProvider();
                if (parent == null)
                {
                    Debug.LogError("GlbLoader: No parent transform available for GLB instantiation.");
                    yield break;
                }

                glbRoot = new GameObject(Path.GetFileNameWithoutExtension(url));
                glbRoot.transform.SetParent(parent, false);

                var materialGenerator = new NL3DMaterialGenerator();
                var logger = new GLTFast.Logging.ConsoleLogger();
                gltf = new GltfImport(null, null, materialGenerator, logger);

                Task<bool> loadTask = null;
                string requestUrl = url;

                if (IsRemoteUrl(url))
                {
                    requestUrl = resolveUrl(url);
                    if (string.IsNullOrEmpty(requestUrl))
                    {
                        Debug.LogError($"GlbLoader: ResolveUrl returned empty for {url}");
                        yield break;
                    }

                    if (IsB3dmPath(requestUrl))
                    {
                        byte[] remoteBytes = null;
                        using (UnityWebRequest webRequest = UnityWebRequest.Get(requestUrl))
                        {
                            yield return webRequest.SendWebRequest();
                            if (webRequest.result != UnityWebRequest.Result.Success)
                            {
                                Debug.LogError($"GlbLoader: Failed to download {requestUrl}: {webRequest.error}");
                                yield break;
                            }
                            remoteBytes = webRequest.downloadHandler.data;
                        }

                        if (!TryExtractGlbFromB3dmBytes(remoteBytes, out var glbBytes))
                        {
                            Debug.LogError($"GlbLoader: Could not convert B3DM from {requestUrl}");
                            yield break;
                        }

                        loadTask = gltf.LoadGltfBinary(glbBytes);
                    }
                    else
                    {
                        loadTask = gltf.Load(requestUrl);
                    }
                }
                else
                {
                    if (!File.Exists(url))
                    {
                        Debug.LogWarning($"GlbLoader: Local GLB file not found: {url}");
                        yield break;
                    }

                    byte[] data;
                    try
                    {
                        data = File.ReadAllBytes(url);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"GlbLoader: Failed to read {url}: {ex.Message}");
                        yield break;
                    }

                    if (IsB3dmPath(url))
                    {
                        if (!TryExtractGlbFromB3dmBytes(data, out data))
                        {
                            Debug.LogError($"GlbLoader: Could not convert local B3DM {url}");
                            yield break;
                        }
                    }

                    loadTask = gltf.LoadGltfBinary(data);
                }

                if (loadTask == null)
                {
                    Debug.LogError("GlbLoader: No load task created.");
                    yield break;
                }

                yield return WaitForTask(loadTask);

                if (TryGetTaskException(loadTask, out var loadException))
                {
                    Debug.LogError($"GlbLoader: Exception loading GLB {requestUrl}: {loadException.Message}");
                    yield break;
                }

                if (!loadTask.Result)
                {
                    Debug.LogError($"GlbLoader: Loading GLB failed {requestUrl}");
                    yield break;
                }

                for (int sceneIndex = 0; sceneIndex < gltf.SceneCount; sceneIndex++)
                {
                    Task instantiateTask = gltf.InstantiateSceneAsync(glbRoot.transform, sceneIndex);
                    yield return WaitForTask(instantiateTask);

                    if (TryGetTaskException(instantiateTask, out var instantiateException))
                    {
                        Debug.LogError($"GlbLoader: Exception instantiating scene {sceneIndex} for {requestUrl}: {instantiateException.Message}");
                        yield break;
                    }
                }

                if (applyTransformProvider())
                {
                    ApplyCsvTransform(glbRoot.transform, position, rotation, scale);
                }
                else if (centerUsingBoundsProvider())
                {
                    CenterSceneOnBounds(glbRoot);
                }
                else
                {
                    glbRoot.transform.SetPositionAndRotation(position, rotation);
                    glbRoot.transform.localScale = scale;
                }
                success = true;
                activeImports.Add(gltf);
            }
            finally
            {
                if (!success && glbRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(glbRoot);
                }

                if (!success)
                {
                    gltf?.Dispose();
                }

                onCompleted?.Invoke(success);
            }
        }

        internal IEnumerator LoadGlb(string url, Vector3 position, Action<bool> onCompleted = null)
        {
            return LoadGlb(url, position, Quaternion.identity, Vector3.one, onCompleted);
        }

        internal void UnloadGlb()
        {
            Transform parent = parentProvider();
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }

            for (int i = activeImports.Count - 1; i >= 0; i--)
            {
                activeImports[i]?.Dispose();
            }
            activeImports.Clear();
        }

        private static IEnumerator WaitForTask(Task task)
        {
            if (task == null)
            {
                yield break;
            }

            while (!task.IsCompleted)
            {
                yield return null;
            }
        }

        private static bool TryGetTaskException(Task task, out Exception exception)
        {
            exception = null;
            if (task == null)
            {
                return false;
            }

            if (task.IsFaulted)
            {
                exception = task.Exception?.InnerException ?? task.Exception;
                return true;
            }

            if (task.IsCanceled)
            {
                exception = new TaskCanceledException(task);
                return true;
            }

            return false;
        }

        private static bool IsRemoteUrl(string path)
        {
            return !string.IsNullOrEmpty(path) && path.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsB3dmPath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.EndsWith(".b3dm", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryExtractGlbFromB3dmBytes(byte[] sourceBytes, out byte[] glbBytes)
        {
            glbBytes = null;
            if (sourceBytes == null || sourceBytes.Length < 4)
            {
                return false;
            }

            string magic = Encoding.UTF8.GetString(sourceBytes, 0, Math.Min(4, sourceBytes.Length));
            if (!magic.Equals("b3dm", StringComparison.OrdinalIgnoreCase))
            {
                glbBytes = sourceBytes;
                return true;
            }

            try
            {
                using (var memoryStream = new MemoryStream(sourceBytes))
                {
                    var b3dm = B3dmReader.ReadB3dm(memoryStream);
                    var glbData = b3dm.GlbData;
                    RemoveCesiumRtcFromRequiredExtensions(glbData);
                    glbBytes = glbData;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GlbLoader: Failed to process B3DM bytes: {ex.Message}");
                return false;
            }
        }

        private static void RemoveCesiumRtcFromRequiredExtensions(byte[] glbData)
        {
            if (glbData == null || glbData.Length <= 20)
            {
                return;
            }

            int jsonStart = 20;
            int jsonLength = GetJsonChunkLength(glbData);
            if (jsonLength <= 0 || jsonStart + jsonLength > glbData.Length)
            {
                return;
            }

            string jsonString = Encoding.UTF8.GetString(glbData, jsonStart, jsonLength);
            const string extensionsToken = "\"extensionsRequired\"";
            const string cesiumToken = "\"CESIUM_RTC\"";

            int extensionsIndex = jsonString.IndexOf(extensionsToken, StringComparison.Ordinal);
            if (extensionsIndex < 0)
            {
                return;
            }

            int afterToken = extensionsIndex + extensionsToken.Length;
            int arrayEnd = jsonString.IndexOf("]", afterToken, StringComparison.Ordinal);
            if (arrayEnd < 0)
            {
                return;
            }

            int cesiumIndex = jsonString.IndexOf(cesiumToken, afterToken, StringComparison.Ordinal);
            if (cesiumIndex < 0)
            {
                return;
            }

            int cesiumEnd = cesiumIndex + cesiumToken.Length;
            int commaIndex = jsonString.IndexOf(",", afterToken, StringComparison.Ordinal);

            int removalStart = cesiumIndex;
            int removalEnd = cesiumEnd;

            if (commaIndex < 0 || commaIndex > arrayEnd)
            {
                removalStart = Math.Max(extensionsIndex - 1, 0);
                removalEnd = arrayEnd + 1;
            }
            else
            {
                if (commaIndex < cesiumIndex)
                {
                    removalStart = Math.Max(commaIndex, extensionsIndex);
                }
                if (commaIndex > cesiumEnd)
                {
                    removalEnd = Math.Min(commaIndex, jsonString.Length);
                }
            }

            for (int i = removalStart; i < removalEnd; i++)
            {
                int glbIndex = jsonStart + i;
                if (glbIndex >= 0 && glbIndex < glbData.Length)
                {
                    glbData[glbIndex] = 0x20;
                }
            }
        }

        private static int GetJsonChunkLength(byte[] glbData)
        {
            if (glbData == null || glbData.Length < 16)
            {
                return 0;
            }

            return BitConverter.ToInt32(glbData, 12);
        }

        private static void ApplyCsvTransform(Transform root, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root)
            {
                child.localPosition = position;
                child.localRotation = rotation;
                child.localScale = scale;
            }
        }

        private static void CenterSceneOnBounds(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 localCenter = root.transform.InverseTransformPoint(combinedBounds.center);
            if (localCenter == Vector3.zero)
            {
                return;
            }

            var children = new Transform[root.transform.childCount];
            for (int i = 0; i < root.transform.childCount; i++)
            {
                children[i] = root.transform.GetChild(i);
            }

            foreach (Transform child in children)
            {
                child.localPosition -= localCenter;
            }
        }
    }
}
