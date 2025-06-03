using UnityEngine;
using System;
using Netherlands3D.DataTypeAdapters;
using System.Collections.Specialized;
using Netherlands3D.Web;
using Newtonsoft.Json;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(DataTypeChain))]
    public class ForcedParameterService : MonoBehaviour
    {
        public string ForcedCrs { get; private set; }
        private DataTypeChain chain;

        private const string marker = "&nl3d=";

        private void Awake()
        {            
            chain = GetComponent<DataTypeChain>();
            chain.OnPreDownloadLocalCache.AddListener(ProcessUrl);
        }

        private void OnDestroy()
        {
            chain.OnPreDownloadLocalCache.RemoveListener(ProcessUrl);
        }

        public void ProcessUrl(LocalFile file)
        {
            Clear(); //reset whenever a new file is loaded


            string rawUrl = file.SourceUrl;

            var nl3dOptions = ExtractOptionsFromUrl(rawUrl);
            ForcedCrs = nl3dOptions?.crs;

            if (!string.IsNullOrEmpty(ForcedCrs))
            {
                Debug.Log($"[ForcedParameterService] Forced CRS detected: {ForcedCrs}");
            }

            UriBuilder uriBuilder = new UriBuilder(rawUrl);
            var parameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(parameters);
            uriBuilder.RemoveQueryParameter("nl3d");
            file.SourceUrl = uriBuilder.ToString();
        }

        [Serializable]
        public class NL3DOptions
        {
            public string crs;
        }

        public static NL3DOptions ExtractOptionsFromUrl(string url)
        {
            UriBuilder uriBuilder = new UriBuilder(url);
            var parameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(parameters);
            NL3DOptions extraOptions = null;
            string nl3dRaw = parameters["nl3d"];
            if (!string.IsNullOrEmpty(nl3dRaw))
            {
                try
                {
                    extraOptions = JsonConvert.DeserializeObject<NL3DOptions>(nl3dRaw);                    
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse nl3d: {ex.Message}");
                }
            }
            return extraOptions;
        }

        

        public void Clear()
        {
            ForcedCrs = null;
        }
    }
}