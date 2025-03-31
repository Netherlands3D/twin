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

    public class AssetBundleLoader : MonoBehaviour
    {
        [SerializeField] private UI_ProgressIndicator loadingScreenPrefab;
        [SerializeField] private RectTransform loadingScreenCanvas;
        private const string GroupName = "ObjectenBibliotheek";

        public string bundleName;
        public string prefabName;

        private UI_ProgressIndicator loadingScreen;

        private void Awake()
        {
            ProjectData.Current.PrefabLibrary.AddPrefabRuntimeGroup(GroupName);
            LoadAssetFromAssetBundle(bundleName, prefabName, AddAssetToObjectLibrary);
        }

        public void LoadAssetFromAssetBundle(string bundleName, string fileName, Action<GameObject> onAssetLoaded)
        {
            string path = Path.Combine(Application.streamingAssetsPath, bundleName);
            loadingScreen = Instantiate(loadingScreenPrefab, loadingScreenCanvas.transform);
            loadingScreen.ShowProgress(0.1f);
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
            
            loadingScreen.ShowProgress(0.99f);
            loadingScreen.GetComponent<CanvasGroup>()
                .DOFade(0, .4f)
                .OnComplete(() => GameObject.Destroy(loadingScreen.gameObject));
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
                loadingScreen.ShowProgress(request.downloadProgress);

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
                // Show some progress (fake, as LoadFromFile doesn't provide progress)
                loadingScreen.ShowProgress(0.5f);
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

        private void AddAssetToObjectLibrary(GameObject asset)
        {
            // we want only the original asset here and not an instantiation
            HierarchicalObjectLayerGameObject layerObject = asset.GetComponent<HierarchicalObjectLayerGameObject>();
            if (!layerObject) return;

            ProjectData.Current.PrefabLibrary.AddObjectToPrefabRuntimeGroup(GroupName, layerObject);
        }
    }
}
