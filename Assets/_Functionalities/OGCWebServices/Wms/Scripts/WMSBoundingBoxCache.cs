using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Functionalities.OgcWebServices.Shared;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Functionalities.Wms
{
    public class WMSBoundingBoxCache : MonoBehaviour
    {
        public static Dictionary<string, BoundingBoxContainer> BoundingBoxContainers = new();
        private List<string> pendingRequests = new();
        
        private static WMSBoundingBoxCache instance;
        public static WMSBoundingBoxCache Instance
        {
            get
            {
                if (!instance)
                {
                    var go = new GameObject("WMSBoundingBoxLibrary_Instance");
                    instance = go.AddComponent<WMSBoundingBoxCache>();
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

        public void GetBoundingBoxContainer(string url, Action<BoundingBoxContainer> callback)
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
            
            if (!OgcCWebServicesUtility.IsSupportedUrl(new Uri(url), ServiceType.Wms, RequestType.GetCapabilities))
            {
                Debug.LogError("Bounding boxes not in dictionary, and invalid wms url provided");
                callback.Invoke(null);
                return;
            }
            
            //send request for Bounding Boxes
            StartCoroutine(RequestBoundingBoxes(url, callback));
        }

        private IEnumerator WaitForExistingRequestToComplete(string url, Action<BoundingBoxContainer> callback)
        {
            while(pendingRequests.Contains(url))
            {
                yield return null;
            }
            
            callback.Invoke(BoundingBoxContainers[url]);
        }
        
        
        private IEnumerator RequestBoundingBoxes(string url, Action<BoundingBoxContainer> callBack)
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
                GetCapabilitiesRequest wmsRequest = new GetCapabilitiesRequest(new Uri(url), webRequest.downloadHandler.text);
                var bboxContainer = AddWmsBoundingBoxContainer(url, wmsRequest);
                callBack.Invoke(bboxContainer);
            }

            pendingRequests.Remove(url);
        }

        public static BoundingBoxContainer AddWmsBoundingBoxContainer(string url, GetCapabilitiesRequest wmsRequest)
        {
            var bboxContainer = wmsRequest.GetBounds(url);
            BoundingBoxContainers.TryAdd(url, bboxContainer); //use tryadd to avoid issues when adding the same wms twice in the application
            return bboxContainer;
        }
    }
}
