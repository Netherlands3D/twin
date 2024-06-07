using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class DownloadFile : MonoBehaviour
    {
        [SerializeField] private string url;
        public UnityEvent<string> onFileDownloaded = new();
        
        public void DownloadFileFromURL()
        {
            StartCoroutine(Download(url));
        }
        
        private IEnumerator Download(string url) {
            // var uwr = new UnityWebRequest("https://unity3d.com/", UnityWebRequest.kHttpVerbGET);
            var uwr = UnityWebRequest.Get(url);
            string path = Path.Combine(Application.persistentDataPath, "test.json");
            
            Debug.Log("downloading from: " + url);
            Debug.Log("downloading to: " + path);
            print("start frame: " + Time.frameCount);
            uwr.downloadHandler = new DownloadHandlerFile(path);
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
                Debug.LogError(uwr.error);
            else
                onFileDownloaded.Invoke(path);
            print("end frame: " + Time.frameCount);
        }
    }
}
