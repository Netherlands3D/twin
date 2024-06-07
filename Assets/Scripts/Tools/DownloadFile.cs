using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class DownloadFile : MonoBehaviour
    {
        public string URL { get; set; }
        public UnityEvent<string> onFileDownloaded = new();
        public UnityEvent<string> onFileDownloadFailed = new();

        //for in the inspector
        public void SetURL(string url)
        {
            URL = url;
        }

        public void DownloadFileFromURL()
        {
            StartCoroutine(Download(URL));
        }

        private IEnumerator Download(string url)
        {
            // var uwr = new UnityWebRequest("https://unity3d.com/", UnityWebRequest.kHttpVerbGET);
            var uwr = UnityWebRequest.Get(url);
            string path = Path.Combine(Application.persistentDataPath, "test.json");

            Debug.Log("downloading from: " + url);
            Debug.Log("downloading to: " + path);
            print("start frame: " + Time.frameCount);
            uwr.downloadHandler = new DownloadHandlerFile(path);
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
                onFileDownloadFailed.Invoke(uwr.error);
            else
                onFileDownloaded.Invoke(path);
            print("end frame: " + Time.frameCount);
        }
    }
}