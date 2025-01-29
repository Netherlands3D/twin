using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Functionalities.Wfs
{
    public class WFSBoundingBoxContainer
    {
        public string url;
        public BoundingBox GlobalBoundingBox;
        public Dictionary<string, BoundingBox> LayerBoundingBoxes = new();
    }
    
    public class WFSBoundingBoxLibrary : MonoBehaviour
    {
        public static Dictionary<string, WFSBoundingBoxContainer> BoundingBoxContainers = new();
        private List<string> pendingRequests = new();
        
        private static WFSBoundingBoxLibrary instance;
        public static WFSBoundingBoxLibrary Instance
        {
            get
            {
                if (!instance)
                {
                    var go = new GameObject("WFSBoundingBoxLibrary_Instance");
                    instance = go.AddComponent<WFSBoundingBoxLibrary>();
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

        public void GetBoundingBoxContainer(string url, Action<WFSBoundingBoxContainer> callback)
        {
            if (pendingRequests.Contains(url))
            {
                StartCoroutine(WaitForExistingRequestToComplete(url, callback));
                return;
            }
            
            if (!WFSRequest.IsValidWFSURL(url))
            {
                callback.Invoke(null);
                return;
            }

            if (BoundingBoxContainers.ContainsKey(url))
            {
                callback.Invoke(BoundingBoxContainers[url]);
                return;
            }
            
            StartCoroutine(GetBoundingBoxes(url, callback));
        }

        private IEnumerator WaitForExistingRequestToComplete(string url, Action<WFSBoundingBoxContainer> callback)
        {
            while(pendingRequests.Contains(url))
            {
                yield return null;
            }
            
            callback.Invoke(BoundingBoxContainers[url]);
        }
        
        
        private IEnumerator GetBoundingBoxes(string url, Action<WFSBoundingBoxContainer> callBack)
        {
            pendingRequests.Add(url);
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Could not download bounding boxes of {url}");
            }
            else
            {
                WFSRequest wfsRequest = new WFSRequest(url, webRequest.downloadHandler.text);

                var bboxContainer = new WFSBoundingBoxContainer();
                var globalBounds = wfsRequest.GetWFSBounds();
                bboxContainer.GlobalBoundingBox = globalBounds;
                
                foreach (var feature in wfsRequest.GetFeatureTypes())
                {
                    bboxContainer.LayerBoundingBoxes.TryAdd(feature.Name, feature.BoundingBox);
                }                
                
                BoundingBoxContainers.Add(url, bboxContainer);
                callBack.Invoke(bboxContainer);
            }

            pendingRequests.Remove(url);
        }
    }
}
