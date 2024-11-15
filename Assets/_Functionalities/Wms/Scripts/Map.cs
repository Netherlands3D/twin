using System;
using Netherlands3D.Web;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.Wms
{
    public struct Map
    {
        public string name;
        public string version;
        public string spatialReferenceType;
        public string spatialReference;
        public string style;
        public int width;
        public int height;
        public bool transparent;
        
        public Uri CreateRequestUrlBasedOn(string sourceUrl)
        {
            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(sourceUrl);

            // Set the required query parameters for the GetMap request
            uriBuilder.SetQueryParameter("service", "WMS");
            uriBuilder.SetQueryParameter("version", version);
            uriBuilder.SetQueryParameter("request", "GetMap");

            uriBuilder.SetQueryParameter("layers", Uri.EscapeDataString(name));
            uriBuilder.SetQueryParameter("styles", style);
            uriBuilder.SetQueryParameter(spatialReferenceType, spatialReference);
            uriBuilder.SetQueryParameter("bbox", "{0}"); // Bbox value is injected by ImageProjectionLayer
            uriBuilder.SetQueryParameter("width", width.ToString());
            uriBuilder.SetQueryParameter("height", height.ToString());
            
            string format = GetParamValueFromSourceUrl(sourceUrl, "format");
            
            format = UnityWebRequest.UnEscapeURL(format);
            if (format != "image/png" && format != "image/jpeg")
            {
                format = "image/png";
            }
            uriBuilder.SetQueryParameter("format", format);
            
            if (!sourceUrl.Contains("transparent="))
            {
                uriBuilder.SetQueryParameter("transparent", transparent ? "true" : "false");
            }

            return uriBuilder.Uri;
        }
        
        private static string GetParamValueFromSourceUrl(string sourceUrl, string param)
        {
            string value = string.Empty;
            string p = param + "=";

            if (!sourceUrl.ToLower().Contains(p)) return value;

            return sourceUrl.ToLower().Split(p)[1].Split("&")[0];
        }
    }
}