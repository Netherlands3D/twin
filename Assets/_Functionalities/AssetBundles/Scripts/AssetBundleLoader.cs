using ICSharpCode.SharpZipLib.Core;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Projects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
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

        private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();


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
                    string fName = Path.GetFileName(name).ToLower();
                    if (fName != fileName.ToLower()) continue;
                    
                    GameObject asset = bundle.LoadAsset<GameObject>(name);
                    if (asset != null)
                    {
                        GameObject copy = Instantiate(asset); //works only for a copy!
#if UNITY_EDITOR
                        FixShadersForEditor(copy);
#endif
                        onAssetLoaded(copy);
                    }

                   // bundle.Unload(false);
                    return;
                }
            }

            StartCoroutine(GetAssetBundle(path, bundleName, OnAssetBundleLoaded));
        }

        private void FixShadersForEditor(GameObject asset)
        {
            //the following fixes the pink bug in editor shaders                     
            MeshRenderer[] renderers = asset.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.shader = Shader.Find(renderer.material.shader.name);
                }
                else
                {
                    renderer.sharedMaterial.shader = Shader.Find(renderer.sharedMaterial.shader.name);
                }

                if(renderer.materials != null)
                {
                    Material[] mats = renderer.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i].shader = Shader.Find(renderer.materials[i].shader.name);
                    }
                    renderer.materials = mats;
                }
                else if(renderer.sharedMaterials != null)
                {
                    Material[] mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i].shader = Shader.Find(renderer.sharedMaterials[i].shader.name);
                    }
                    renderer.sharedMaterials = mats;
                }
            }
        }

        public IEnumerator GetAssetBundle(string path, string bundleName, UnityAction<AssetBundle> callBack)
        {
            string key = path + bundleName;
            if (!loadedBundles.ContainsKey(key))
            {
                loadedBundles.Add(key, null);
                UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(path);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(request.error + "path" + path);
                    loadedBundles.Remove(key);
                    callBack?.Invoke(null);
                }
                else
                {
                    // Get downloaded asset bundle
                    AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
                    loadedBundles[key] = bundle;
                    callBack?.Invoke(bundle);
                }
            }
            else
            {
                if (loadedBundles[key] != null)
                    callBack?.Invoke(loadedBundles[key]);                
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
