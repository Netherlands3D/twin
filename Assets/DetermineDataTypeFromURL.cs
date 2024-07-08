using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class DetermineDataTypeFromURL : MonoBehaviour
    {
        public UnityEvent<string> onFileWithExtentionDetermined = new();
        public UnityEvent<string> onWFSServiceDetermined = new();

        public UnityEvent<string> onNotSupportedDetermined = new();

        public void Determine(string url)
        {
            // Check if this url is a singular file with a file extention by checking if there is a dot in the url
            int lastDotIndex = url.LastIndexOf(".");
            int lastSlashIndex = url.LastIndexOf("/");
            if (lastDotIndex > lastSlashIndex)
            {
                string fileExtension = url.Substring(lastDotIndex + 1);
                if (!string.IsNullOrEmpty(fileExtension))
                {
                    onFileWithExtentionDetermined.Invoke(url);
                    return;
                }
            }

            // Check if this url is a WFS service
            if (IsWFSService(url))
            {
                onWFSServiceDetermined.Invoke(url);
                return;
            }
        }

        //A very basic check to see if this might be a WFS service
        public bool IsWFSService(string url)
        {
            return url.ToLower().Contains("request=getcapabilities") || url.ToLower().Contains("service=getfeature");

            // Check service query parameter too
            // Parse XML getcapabilities to check if it is a WFS service, WMS or WMTS
            // GetFeature uit Operation
        }
    }
}
