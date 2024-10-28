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
        public string bundleName;
        public string prefabName;
        public Vector3 spawnPosition;

        public static List<HierarchicalObjectLayerGameObject> hierarchicalObjectLayerGameObjects = new List<HierarchicalObjectLayerGameObject>();

        private void Awake()
        {
            ProjectData.Current.PrefabLibrary.AddPrefabGroupRuntime("ObjectenBibliotheek");
            OnProjectDataChanged(ProjectData.Current);
        }

        private void Start()
        {
            //ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);
        }

        private void OnProjectDataChanged(ProjectData projectData)
        {
            //we want only the original asset here and not an instantiation
            LoadAssetFromAssetBundle(bundleName, prefabName, Vector3.zero, asset =>
            {
                HierarchicalObjectLayerGameObject layerObject = asset.GetComponent<HierarchicalObjectLayerGameObject>();
                if (layerObject != null)
                {                
                    projectData.PrefabLibrary.AddObjectToPrefabGroupRuntime("ObjectenBibliotheek", layerObject);
                    if(!hierarchicalObjectLayerGameObjects.Contains(layerObject))
                        hierarchicalObjectLayerGameObjects.Add(layerObject);
                }
            });
        }

        public void LoadAssetFromAssetBundle(string bundleName, string fileName, Vector3 position, Action<GameObject> onLoaded)
        {
            string path = Path.Combine(Application.streamingAssetsPath, bundleName);
            StartCoroutine(GetAssetBundle(path, bundle =>
            {
                string[] names = bundle.GetAllAssetNames();
                foreach (string n in names)
                {
                    if (Path.GetFileName(n) == fileName.ToLower())
                    {
                        GameObject asset = bundle.LoadAsset<GameObject>(n);
                        if (asset != null)
                        {
#if UNITY_EDITOR
                            FixShadersForEditor(asset);
#endif
                            onLoaded(asset);
                        }
                        bundle.Unload(false);
                        return;
                    }
                }
            }));
        }

        public void CreateAssetFromAssetBundle(string bundleName, string fileName, Vector3 position, Action<GameObject> onLoaded)
        {
            string path = Path.Combine(Application.streamingAssetsPath, bundleName);
            StartCoroutine(GetAssetBundle(path, bundle =>
            {
                string[] names = bundle.GetAllAssetNames();
                foreach (string n in names)
                {
                    if (Path.GetFileName(n) == fileName.ToLower())
                    {
                        GameObject asset = bundle.LoadAsset<GameObject>(n);
                        if (asset != null)
                        {
                            GameObject model = Instantiate(asset);
#if UNITY_EDITOR
                            FixShadersForEditor(model);
#endif
                            onLoaded(model);
                        }
                        bundle.Unload(false);
                        return;
                    }
                }
            }));
        }

        private void FixShadersForEditor(GameObject asset)
        {
            //the following fixes the pink bug in editor shaders                     
            MeshRenderer[] renderers = asset.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                if(renderer.material != null)
                    renderer.material.shader = Shader.Find(renderer.material.shader.name);
                else
                    renderer.sharedMaterial.shader = Shader.Find(renderer.sharedMaterial.shader.name);
            }
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

        private void OnDestroy()
        {
            //ProjectData.Current.OnDataChanged.RemoveListener(OnProjectDataChanged);            
            hierarchicalObjectLayerGameObjects.Clear();
        }
    }
}
