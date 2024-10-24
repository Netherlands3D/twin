using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class AssetBundleLoader : MonoBehaviour
    {
        private void Start()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "zuidoostbundle");
            StartCoroutine(GetAndroidBundle(path, bundle =>
            {
                string[] names = bundle.GetAllAssetNames();
                foreach (string n in names)
                {
                    if (n.Contains(tag))
                    {
                        GameObject asset = bundle.LoadAsset<GameObject>(n);
                        
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
