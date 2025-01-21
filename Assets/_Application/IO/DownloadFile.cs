using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.IO
{
    public class DownloadFile : MonoBehaviour
    {
        [SerializeField] private string[] validFileExtensions;
        public string URL { get; set; }
        public UnityEvent<string> onFileDownloaded = new();
        public UnityEvent<string> onFileDownloadFailed = new();

        private void Awake()
        {
            for (var i = 0; i < validFileExtensions.Length; i++)
            {
                var extension = validFileExtensions[i].Trim();
                if (!extension.StartsWith('.'))
                {
                    extension = "." + extension;
                }

                validFileExtensions[i] = extension;
            }
        }

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
            string filename = string.Empty;
            try
            {
                Uri uri = new Uri(url);
                filename = Path.GetFileName(uri.AbsolutePath);

                if (filename == string.Empty)
                {
                    Debug.LogError("The provided URL does not contain a valid filename: " + url);
                    onFileDownloadFailed.Invoke("The provided URL does not contain a valid filename: " + url);
                    yield break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                onFileDownloadFailed.Invoke(e.Message);
                yield break;
            }

            var extension = Path.GetExtension(filename);
            print(extension);
            if (!validFileExtensions.Contains(extension))
            {
                Debug.LogError("The provided URL does not contain a file with a valid file extension. File found: " + filename);
                onFileDownloadFailed.Invoke("The provided URL does not contain a file with a valid file extension. File found: " + filename);
                yield break;
            }

            var uwr = UnityWebRequest.Get(url);
            string path = Path.Combine(Application.persistentDataPath, filename);

            Debug.Log("downloading from: " + url);
            Debug.Log("downloading to: " + path);
            uwr.downloadHandler = new DownloadHandlerFile(path);
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(uwr.error);
                onFileDownloadFailed.Invoke(uwr.error);
            }
            else
            {
                onFileDownloaded.Invoke(path);
            }
        }
    }
}