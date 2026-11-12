using Netherlands3D.Twin.Projects;
using System;
using System.Collections;
using System.IO;
using DG.Tweening;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.UI.Loader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Functionalities.AssetBundles
{
    //for now this is a test scenario loading script for the zuidoostbundle 
    //this could be written as a controller to load assetbundles    

    //step 1, put all  your assets in the assetbundleassets folder
    //step 2, give all your assets a unified tag (create for the first asset the new tag)
    //step 3, in the menu -> assets -> click Build assetbundles
    //step 4, your assetbundle will be put in streamingassets
    //step 5, use LoadAssetFromAssetBundle from any script to instantiate your prefab from the bundle

    //step 6, for any change in any prefab within an assetbundle you need to rebuild the assetbundles

    public class AssetBundleService : MonoBehaviour
    {
        public UnityEvent OnAssetsLoaded;

        public void LoadAssetFromAssetBundle(string bundleName, string fileName, Action<GameObject> onAssetLoaded)
        {
            string path = Path.Combine(Application.streamingAssetsPath, bundleName);
            StartCoroutine(GetAssetBundle(path, bundle => OnAssetBundleLoaded(bundle, fileName, onAssetLoaded)));
        }

        void OnAssetBundleLoaded(AssetBundle bundle, string fileName, Action<GameObject> onAssetLoaded)
        {
            string[] names = bundle.GetAllAssetNames();
            foreach (string name in names)
            {
                // if the file is not a match, move on
                if (Path.GetFileName(name) != fileName.ToLower()) continue;

                GameObject asset = bundle.LoadAsset<GameObject>(name);
                if (asset != null)
                {
                    FixShaders(asset);
                    onAssetLoaded(asset);
                }

                bundle.Unload(false);
                break;
            }
        }

        private void FixShaders(GameObject asset)
        {
            //the following fixes the pink bug in editor shaders                     
            MeshRenderer[] renderers = asset.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                if (!IsPrefab(asset) && renderer.material)
                {
                    renderer.material.shader = Shader.Find(renderer.material.shader.name);
                    continue;
                }

                renderer.sharedMaterial.shader = Shader.Find(renderer.sharedMaterial.shader.name);
            }
        }

        private static bool IsPrefab(GameObject gameObject)
        {
            // A prefab will have a `transform` but no parent if it's a root prefab in play mode
            return string.IsNullOrEmpty(gameObject.scene.name);
        }

        private IEnumerator GetAssetBundle(string path, UnityAction<AssetBundle> callBack)
        {
#if !UNITY_EDITOR
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(path);
            request.SendWebRequest();
            while (!request.isDone)
            {
                yield return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
            }
            else
            {
                // Get downloaded asset bundle
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
                callBack?.Invoke(bundle);
            }
#else
            // Use AssetBundle.LoadFromFile for the Editor, this fixes a "Cannot connect to destination host" error on the Mac editor
            if (File.Exists(path))
            {
                yield return null;
                
                AssetBundle bundle = AssetBundle.LoadFromFile(path);
                if (bundle == null)
                {
                    Debug.LogError("Failed to load AssetBundle from path: " + path);
                }
                else
                {
                    callBack?.Invoke(bundle);
                }
            }
            else
            {
                Debug.LogError("File does not exist at path: " + path);
            }
#endif
        }
        
        public void InstantiatePrefab(
            string bundleName,
            string prefabName,
            Transform parent,
            Action<GameObject> onInstantiated)
        {
            StartCoroutine(InstantiatePrefabRoutine(
                bundleName,
                prefabName,
                parent,
                onInstantiated
            ));
        }

        private IEnumerator InstantiatePrefabRoutine(
            string bundleName,
            string prefabName,
            Transform parent,
            Action<GameObject> onInstantiated)
        {
            string path = Path.Combine(
                Application.streamingAssetsPath,
                bundleName
            );

            AssetBundle bundle = null;

            yield return GetAssetBundle(path, loadedBundle =>
            {
                bundle = loadedBundle;
            });

            if (bundle == null)
            {
                Debug.LogError($"Failed to load AssetBundle: {bundleName}");
                onInstantiated?.Invoke(null);
                OnAssetsLoaded?.Invoke();
                yield break;
            }

            GameObject instance = null;

            try
            {
                string[] assetNames = bundle.GetAllAssetNames();

                string targetName = Path.GetFileNameWithoutExtension(
                    prefabName
                );

                foreach (string assetName in assetNames)
                {
                    string assetFileName = Path.GetFileNameWithoutExtension(
                        assetName
                    );

                    if (!string.Equals(
                        assetFileName,
                        targetName,
                        StringComparison.OrdinalIgnoreCase))
                        continue;

                    GameObject prefab = bundle.LoadAsset<GameObject>(assetName);

                    if (prefab == null)
                    {
                        Debug.LogError($"Could not load prefab: {prefabName}");
                        break;
                    }

                    instance = Instantiate(prefab, parent, false);
                    break;
                }

                if (instance == null)
                {
                    Debug.LogError(
                        $"Prefab '{prefabName}' not found in bundle '{bundleName}'"
                    );
                }
            }
            finally
            {
                // Release the bundle container, but preserve loaded assets.
                bundle.Unload(false);
            }

            onInstantiated?.Invoke(instance);
            OnAssetsLoaded?.Invoke();
        }
        
        public (string bundleName, string prefabName) ParseAssetBundleUri(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (uri.Scheme != "streamingassets")
                throw new ArgumentException("Not a StreamingAssets URI.", nameof(uri));

            string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2)
                throw new FormatException($"Invalid AssetBundle URI: {uri}");

            string bundleName = Uri.UnescapeDataString(segments[0]);
            string prefabName = Uri.UnescapeDataString(segments[1]);

            return (bundleName, prefabName);
        }
    }
}
