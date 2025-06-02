using UnityEngine;
using System;
using Netherlands3D.DataTypeAdapters;

namespace Netherlands3D.Twin
{
    public class ForcedParameterService : MonoBehaviour
    {
        public static ForcedParameterService Instance { get; private set; }

        public string ForcedCrs { get; private set; }

        private const string marker = "&nl3d=";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;            
        }

        public void ProcessUrl(LocalFile file)
        {
            string rawUrl = file.SourceUrl;

            var nl3dOptions = ExtractOptionsFromUrl(rawUrl);
            ForcedCrs = nl3dOptions?.crs;

            if (!string.IsNullOrEmpty(ForcedCrs))
            {
                Debug.Log($"[ForcedParameterService] Forced CRS detected: {ForcedCrs}");
            }

            file.SourceUrl = RemoveMarkerFromUrl(rawUrl);
        }

        [Serializable]
        public class NL3DOptions
        {
            public string crs;
        }

        public static NL3DOptions ExtractOptionsFromUrl(string url)
        {
            int start = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;

            int jsonStart = start + marker.Length;
            string jsonRaw = url.Substring(jsonStart);
                        
            int nextParam = jsonRaw.IndexOf('&');
            if (nextParam >= 0)
                jsonRaw = jsonRaw.Substring(0, nextParam);

            try
            {
                string decoded = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(jsonRaw);
                return JsonUtility.FromJson<NL3DOptions>(decoded);
            }
            catch
            {
                Debug.LogWarning("unable to parse nl3d parameter");
                return null;
            }
        }

        public static string RemoveMarkerFromUrl(string url)
        {
            int start = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return url;

            int end = url.IndexOf('&', start + 1);
            if (end > 0)
                return url.Remove(start, end - start);
            else
                return url.Substring(0, start);
        }

        public void Clear()
        {
            ForcedCrs = null;
        }
    }
}