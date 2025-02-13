using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.OgcWebServices.Shared
{
    public class BoundingBoxCache : MonoBehaviour
    {
        public static Dictionary<string, BoundingBoxContainer> BoundingBoxContainers = new();
        private List<string> pendingRequests = new();
        
        private static BoundingBoxCache instance;
        public static BoundingBoxCache Instance
        {
            get
            {
                if (!instance)
                {
                    var go = new GameObject("BoundingBoxLibrary_Instance");
                    instance = go.AddComponent<BoundingBoxCache>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance)
            {
                Debug.LogError("An instance already exists, destroying this one: " + this);
                Destroy(this);
                return;
            }
            instance = this;
        }
        
        private IEnumerator WaitForExistingRequestToComplete(string url, Action<BoundingBoxContainer> callback)
        {
            while(pendingRequests.Contains(url))
            {
                yield return null;
            }
            
            callback.Invoke(BoundingBoxContainers[url]);
        }
        
        public void GetBoundingBoxContainer(Uri uri, Func<string, IGetCapabilities> stringToIGetCapabilitiesFactory, Action<BoundingBoxContainer> callback)
        {
            string urlString = uri.ToString();
            if (BoundingBoxContainers.ContainsKey(urlString))
            {
                callback.Invoke(BoundingBoxContainers[urlString]);
                return;
            } 
    
            if (pendingRequests.Contains(urlString))
            {
                StartCoroutine(WaitForExistingRequestToComplete(urlString, callback));
                return;
            }
    
            if (!OgcWebServicesUtility.IsValidUrl(uri, RequestType.GetCapabilities))
            {
                Debug.LogError("Bounding boxes not in dictionary, and invalid getCapabilities url provided");
                callback.Invoke(null);
                return;
            }
    
            StartCoroutine(RequestBoundingBoxes(uri, stringToIGetCapabilitiesFactory, callback));
        }

        private IEnumerator RequestBoundingBoxes(Uri uri, Func<string, IGetCapabilities> factory, Action<BoundingBoxContainer> callback)
        {
            string url = uri.ToString();
            pendingRequests.Add(url);
    
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Could not download bounding boxes of {url}");
                callback.Invoke(null);
            }
            else
            {
                var getCapabilitiesRequest = factory(webRequest.downloadHandler.text);
                var bboxContainer = AddBoundingBoxContainer(getCapabilitiesRequest);
                callback.Invoke(bboxContainer);
            }

            pendingRequests.Remove(url);
        }
        
        public static BoundingBoxContainer AddBoundingBoxContainer(IGetCapabilities getCapabilities)
        {
            var bounds = getCapabilities.GetBounds();
            BoundingBoxContainers.TryAdd(getCapabilities.GetCapabilitiesUri.ToString(), bounds); //use tryadd to avoid issues when adding the same GetCapabilities twice in the application
            return bounds;
        }
    }
}
