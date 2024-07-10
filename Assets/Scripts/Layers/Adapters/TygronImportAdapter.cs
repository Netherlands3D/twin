using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Security.Cryptography;
using Netherlands3D.Web;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/TygronImportAdapter", fileName = "TygronImportAdapter", order = 0)]
    public class TygronImportAdapter : ScriptableObject
    {
        [SerializeField] private GeoJSONImportAdapter geoJSONImportAdapter;

        public UnityEvent onProjectParsed;
        public UnityEvent onProjectFailed;

        public void OpenProject(string url, MonoBehaviour caller)
        {
            //Create webrerquest to url
            caller.StartCoroutine(OpenProjectCoroutine(url));
        }

        private IEnumerator OpenProjectCoroutine(string url)
        {
            //Create webrerquest to url
            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading Tygron project: " + webRequest.error);
                onProjectFailed.Invoke();
                yield break;
            }
            else{
                //Extract token parameter from url
                var uriBuilder = new UriBuilder(url);
                var queryParameters = new NameValueCollection();
                uriBuilder.TryParseQueryString(queryParameters);
                var token = queryParameters["token"];

                var neighborhoodsUrl = CreateNeighborhoodsUrl(token);

                yield return DownloadNeighborhoods(neighborhoodsUrl, token);
            }
        }
        private string CreateNeighborhoodsUrl(string token)
        {
            var newUrl = "https://engine.tygron.com/api/session/items/neighborhoods/?f=GEOJSON&crs=3857&token=" + token;
            return newUrl;
        }

        private IEnumerator DownloadNeighborhoods(string url, string token)
        {
            //Create webrerquest to url
            Debug.Log("Downloading Neighborhoods");
            Debug.Log(url);
            Debug.Log(token);
            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error DownloadNeighborhoods: " + webRequest.error);
                onProjectFailed.Invoke();
                yield break;
            }
            else
            {
                onProjectParsed.Invoke();

                Debug.Log("Successfully loaded Neighborhoods");
                var randomGuidFilePathJson = Guid.NewGuid().ToString() + ".json";
                var fullPath = Path.Combine(Application.persistentDataPath, randomGuidFilePathJson);
                File.WriteAllText(fullPath, webRequest.downloadHandler.text);
                Debug.Log(webRequest.downloadHandler.text);
                geoJSONImportAdapter.ParseGeoJSON(randomGuidFilePathJson);
            }
        }
    }
}