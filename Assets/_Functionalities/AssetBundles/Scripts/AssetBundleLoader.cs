using ICSharpCode.SharpZipLib.Core;
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

    public class AssetBundleLoader : MonoBehaviour
    {
        public string bundleName;
        public string prefabName;
        public Vector3 spawnPosition;

        private void Start()
        {
            LoadAssetFromAssetBundle(bundleName, prefabName, Vector3.zero);
        }

        public void LoadAssetFromAssetBundle(string bundleName, string fileName, Vector3 position)
        {
            string path = Path.Combine(Application.streamingAssetsPath, bundleName);
            StartCoroutine(GetAndroidBundle(path, bundle =>
            {
                string[] names = bundle.GetAllAssetNames();
                foreach (string n in names)
                {
                    if (Path.GetFileName(n) == fileName)
                    {
                        GameObject asset = bundle.LoadAsset<GameObject>(n);
                        if (asset != null)
                        {
                            GameObject model = Instantiate(asset);
                            model.transform.SetParent(transform, false);

                            //the following fixes the pink bug in editor shaders                     
                            MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
                            foreach (MeshRenderer renderer in renderers)
                            {
                                renderer.material.shader = Shader.Find(renderer.material.shader.name);
                            }                            
                        }
                        return;
                    }
                }
            }));
        }

        public IEnumerator GetAndroidBundle(string path, UnityAction<AssetBundle> callBack)
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
    }
}
