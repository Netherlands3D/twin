using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Tiles3D
{
    public class DownloadHelper : MonoBehaviour
    {
        public void downloadData(string url, System.Action<DownloadHandler> returnTo)
        {
            StartCoroutine(DownloadData(url, returnTo));
        }

        IEnumerator DownloadData(string url, System.Action<DownloadHandler> returnTo)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"Could not load tileset from url:{url} Error:{www.error}");
            }
            else
            {
                returnTo.Invoke(www.downloadHandler);
            }
            returnTo.Invoke(null);
        }
    }
}
