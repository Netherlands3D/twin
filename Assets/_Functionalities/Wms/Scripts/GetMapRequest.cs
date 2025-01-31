using System;
using System.Collections.Specialized;
using KindMen.Uxios;
using Netherlands3D.Web;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    public class GetMapRequest : BaseRequest
    {
        public static bool Supports(Uri url)
        {
            return IsSupportedUrl(url, "GetMap");
        }
        
        public GetMapRequest(Uri sourceUrl, string cachedBodyFilePath) : base(sourceUrl, cachedBodyFilePath)
        {
        }

        internal MapFilters CreateMapFromCapabilitiesUrl(Uri url, int width, int height, bool transparent)
        {
            var parameters = QueryString.Decode(url.Query);
            var version = parameters.Get("version");
            if (string.IsNullOrEmpty(version))
            {
                version = defaultFallbackVersion;
                Debug.LogWarning("WMS version could not be determined, defaulting to " + defaultFallbackVersion);
            }
            
            var wmsParam = new MapFilters
            {
                name = parameters.Get("layers"),
                spatialReferenceType = MapFilters.SpatialReferenceTypeFromVersion(new Version(version)),
                spatialReference = defaultCoordinateSystemReference,
                style = parameters.Get("styles"),
                version = version,
                width = width,
                height = height,
                transparent = transparent
            };

            var crs = parameters.Get(wmsParam.spatialReferenceType);
            wmsParam.spatialReference = !string.IsNullOrEmpty(crs) ? crs : defaultCoordinateSystemReference;

            return wmsParam;
        }

        public static string GetLayerNameFromURL(string wmsUrl)
        {
            var uri = new Uri(wmsUrl);
            var nvc = new NameValueCollection();
            uri.TryParseQueryString(nvc);
            var featureLayerName = nvc.Get("layers");
            return featureLayerName;
        }
    }
}