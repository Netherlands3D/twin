using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Projects;
using System;
using System.Collections;
using System.IO;
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

    //for any change in any prefab within an assetbundle you need to rebuild the assetbundles

    public class AssetBundleLoader : MonoBehaviour
    {
        private const string GroupName = "ObjectenBibliotheek";

        public string bundleName;
        public string prefabName;

        private void Awake()
        {
            ProjectData.Current.PrefabLibrary.AddPrefabRuntimeGroup(GroupName);
            LoadAssetFromAssetBundle(bundleName, prefabName, AddAssetToObjectLibrary);
        }

        public void LoadAssetFromAssetBundle(string bundleName, string fileName, Action<GameObject> onAssetLoaded)
        {
            string path = Path.Combine(Application.streamingAssetsPath, bundleName);

            void OnAssetBundleLoaded(AssetBundle bundle)
            {
                string[] names = bundle.GetAllAssetNames();
                foreach (string name in names)
                {
                    // if the file is not a match, move on
                    if (Path.GetFileName(name) != fileName.ToLower()) continue;
                    
                    GameObject asset = bundle.LoadAsset<GameObject>(name);
                    if (asset != null)
                    {
#if UNITY_EDITOR
                        FixShadersForEditor(asset);
#endif
                        onAssetLoaded(asset);
                    }

                    bundle.Unload(false);
                    return;
                }
            }

            StartCoroutine(GetAssetBundle(path, OnAssetBundleLoaded));
        }

        private void FixShadersForEditor(GameObject asset)
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

        public IEnumerator GetAssetBundle(string path, UnityAction<AssetBundle> callBack)
        {
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(path);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                callBack?.Invoke(null);
            }
            else
            {
                // Get downloaded asset bundle
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
                callBack?.Invoke(bundle);
            }
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
