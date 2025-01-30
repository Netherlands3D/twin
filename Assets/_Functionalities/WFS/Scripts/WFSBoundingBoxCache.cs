using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Functionalities.Wfs
{
    public class WFSBoundingBoxContainer
    {
        public string url;
        public BoundingBox GlobalBoundingBox;
        public Dictionary<string, BoundingBox> LayerBoundingBoxes = new();

        public WFSBoundingBoxContainer(string url)
        {
            this.url = url;
        }
    }
    
    public class WFSBoundingBoxCache : MonoBehaviour
    {
        public static Dictionary<string, WFSBoundingBoxContainer> BoundingBoxContainers = new();
        private List<string> pendingRequests = new();
        
        private static WFSBoundingBoxCache instance;
        public static WFSBoundingBoxCache Instance
        {
            get
            {
                if (!instance)
                {
                    var go = new GameObject("WFSBoundingBoxLibrary_Instance");
                    instance = go.AddComponent<WFSBoundingBoxCache>();
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
            if (BoundingBoxContainers.ContainsKey(url))
            {
                // bboxes in dictionary already
                callback.Invoke(BoundingBoxContainers[url]);
                return;
            } 
            
            if (pendingRequests.Contains(url))
            {
                //request for this url already sent, wait for it to complete and return its bboxes
                StartCoroutine(WaitForExistingRequestToComplete(url, callback));
                return;
            }
            
            if (!WFSRequest.IsValidWFSURL(url))
            {
                Debug.LogError("Bounding boxes not in dictionary, and invalid wfs url provided");
                callback.Invoke(null);
                return;
            }
            
            //send request for Bounding Boxes
            StartCoroutine(RequestBoundingBoxes(url, callback));
        }

        private IEnumerator WaitForExistingRequestToComplete(string url, Action<WFSBoundingBoxContainer> callback)
        {
            while(pendingRequests.Contains(url))
            {
                yield return null;
            }
            
            callback.Invoke(BoundingBoxContainers[url]);
        }
        
        
        private IEnumerator RequestBoundingBoxes(string url, Action<WFSBoundingBoxContainer> callBack)
        {
            pendingRequests.Add(url);
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Could not download bounding boxes of {url}");
                callBack.Invoke(null);
            }
            else
            {
                WFSRequest wfsRequest = new WFSRequest(url, webRequest.downloadHandler.text);
                var bboxContainer = AddWfsBoundingBoxContainer(url, wfsRequest);
                callBack.Invoke(bboxContainer);
            }

            pendingRequests.Remove(url);
        }

        public static WFSBoundingBoxContainer AddWfsBoundingBoxContainer(string url, WFSRequest wfsRequest)
        {
            var bboxContainer = new WFSBoundingBoxContainer(url);
            var globalBounds = wfsRequest.GetWFSBounds();
            bboxContainer.GlobalBoundingBox = globalBounds;

            foreach (var feature in wfsRequest.GetFeatureTypes())
            {
                bboxContainer.LayerBoundingBoxes.TryAdd(feature.Name, feature.BoundingBox);
            }

            BoundingBoxContainers.TryAdd(url, bboxContainer); //use tryadd to avoid issues when adding the same wfs twice in the application
            return bboxContainer;
        }
    }
}
