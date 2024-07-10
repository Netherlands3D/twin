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

        private Location location;

        public class Location
        {
            public CenterPoint centerPoint { get; set; }
            public Envelope envelope { get; set; }
            public string format { get; set; }
            public class CenterPoint
            {
                public double x { get; set; }
                public double y { get; set; }
                public string z { get; set; }
            }

            public class Envelope
            {
                public double maxx { get; set; }
                public double maxy { get; set; }
                public double minx { get; set; }
                public double miny { get; set; }
            }        
        } 

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

                // Construct URLS
                var locationUrl = CreateLocationUrl(token);
                var imageUrl = GetImageUrl(token, 67);
                var neighborhoodsUrl = CreateNeighborhoodsUrl(token);

                // Retreive data from different endpoints using the constructed urls
                yield return DownloadLocation(locationUrl);
                //yield return RetrieveSpecificOverlay(imageUrl);
                yield return DownloadNeighborhoods(neighborhoodsUrl);
            }
        }

        private string GetImageUrl(string token,int id)
        {
            var newUrl = $"https://engine.tygron.com/web/overlay.png?id={id}&width=512&height=512&token=" + token;
            return newUrl;
        }

        private IEnumerator RetrieveSpecificOverlay(string imageUrl)
        {
            //Download image texture from png 
            Debug.Log("Downloading Image");
            var webRequest = UnityWebRequestTexture.GetTexture(imageUrl);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error RetrieveSpecificOverlay: " + webRequest.error);
                onProjectFailed.Invoke();
                yield break;
            }
            else
            {
                Debug.Log("Successfully loaded Image");
                var texture = DownloadHandlerTexture.GetContent(webRequest);
                var randomGuidFilePath = Guid.NewGuid().ToString() + ".png";
                var fullPath = Path.Combine(Application.persistentDataPath, randomGuidFilePath);
                File.WriteAllBytes(fullPath, texture.EncodeToPNG());
                
                //Create a new sprite renderer in the scene and assign the texture to it
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                var spriteRenderer = new GameObject("Overlay").AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
            }
        }

        private string CreateLocationUrl(string token)
        {
            var newUrl = "https://engine.tygron.com/api/session/location/?f=GEOJSON&crs=3857&token=" + token;
            return newUrl;
        }

        private string CreateNeighborhoodsUrl(string token)
        {
            var newUrl = "https://engine.tygron.com/api/session/items/neighborhoods/?f=GEOJSON&crs=3857&token=" + token;
            return newUrl;
        }

        private IEnumerator DownloadNeighborhoods(string url)
        {
            //Create webrerquest to url
            Debug.Log("Downloading Neighborhoods");
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

        private IEnumerator DownloadLocation(string url)
        {
            //Create webrerquest to url
            Debug.Log("Downloading Location");
            var webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error DownloadLocation: " + webRequest.error);
                onProjectFailed.Invoke();
                yield break;
            }
            else
            {
                Debug.Log("Successfully loaded Location");
                var jsonLocationText = webRequest.downloadHandler.text;
                location = JsonUtility.FromJson<Location>(jsonLocationText);
            }
        }
    }
}