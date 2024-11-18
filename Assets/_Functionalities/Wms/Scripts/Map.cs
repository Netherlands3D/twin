using System;
using KindMen.Uxios;
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

        public static Map FromUrl(Uri url)
        {
            var queryParameters = QueryString.Decode(url.Query);
            if (queryParameters["request"].ToLower() != "getmap")
            {
                throw new NotSupportedException("Creating a Map from URL is reserved for requests of type GetMap");
            }

            var version = queryParameters["version"];
            var spatialReferenceType = GetMapRequest.SpatialReferenceTypeFromVersion(new Version(version));
            int.TryParse(queryParameters["width"], out var parsedWidth);
            int.TryParse(queryParameters["height"], out var parsedHeight);
            bool.TryParse(queryParameters["transparent"], out var parsedTransparent);

            return new Map
            {
                name = queryParameters["layers"],
                version = version,
                spatialReferenceType = spatialReferenceType,
                spatialReference = queryParameters[spatialReferenceType],
                style = queryParameters["style"],
                width = parsedWidth,
                height = parsedHeight,
                transparent = parsedTransparent,
            };
        }
        
        public Uri ToUrlBasedOn(Uri otherUrl)
        {
            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(otherUrl.AbsoluteUri);
            
            // Grab query parameters from previous url for re-use
            var queryParameters = QueryString.Decode(otherUrl.Query);

            // Set the required query parameters for the GetMap request
            uriBuilder.SetQueryParameter("service", "WMS");
            uriBuilder.SetQueryParameter("version", version);
            uriBuilder.SetQueryParameter("request", "GetMap");

            uriBuilder.SetQueryParameter("layers", name);
            uriBuilder.SetQueryParameter("styles", style);
            uriBuilder.SetQueryParameter(spatialReferenceType, spatialReference);
            uriBuilder.SetQueryParameter("bbox", "{0}"); // Bbox value is injected by WmsTileDataLayer
            uriBuilder.SetQueryParameter("width", width.ToString());
            uriBuilder.SetQueryParameter("height", height.ToString());
            
            string format = queryParameters.Get("format");
            if (format is not "image/png" and not "image/jpeg")
            {
                format = "image/png";
            }
            uriBuilder.SetQueryParameter("format", format);
            
            string transparentField = queryParameters.Get("transparent");
            if (string.IsNullOrEmpty(transparentField))
            {
                uriBuilder.SetQueryParameter("transparent", transparent ? "true" : "false");
            }

            return uriBuilder.Uri;
        }
    }
}