using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Netherlands3D.Web;
using UnityEngine;

namespace Netherlands3D.Functionalities.OgcWebServices.Shared
{
    public static class OgcWebServicesUtility
    {
        public static readonly Dictionary<string, string> DefaultWmsNamespaces = new()
        {
            { "ows", "http://www.opengis.net/ows/1.1" },
            { "wms", "http://www.opengis.net/wms" },
            { "sld", "http://www.opengis.net/sld" },
            { "ms", "http://mapserver.gis.umn.edu/mapserver" },
            { "schemaLocation", "http://www.opengis.net/wms" }
        };

        public static readonly Dictionary<string, string> DefaultWfsNamespaces = new()
        {
            { "ows", "http://www.opengis.net/ows/1.1" },
            { "wfs", "http://www.opengis.net/wfs" },
            { "schemaLocation", "http://www.opengis.net/wfs" }
        };

        public static string CreateGetCapabilitiesURL(string url, string serviceType)
        {
            var uri = new Uri(url);
            var baseUrl = uri.GetLeftPart(UriPartial.Path);
            return $"{baseUrl}?request=GetCapabilities&service={serviceType}";
        }

        public static bool IsValidURL(string url, ServiceType serviceType)
        {
            return url.Contains($"service={serviceType.ToString()}", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValidUrl(Uri url, ServiceType serviceType, RequestType requestType)
        {
            return url.Query.Contains($"service={serviceType}", StringComparison.OrdinalIgnoreCase) && 
                   url.Query.Contains($"request={requestType}", StringComparison.OrdinalIgnoreCase);
        }
        
        public static string GetParameterFromURL(string url, string parameter)
        {
            var uri = new Uri(url);
            var nvc = new NameValueCollection();
            uri.TryParseQueryString(nvc);
            var featureLayerName = nvc.Get(parameter);
            return featureLayerName;
        }
        
        public static string ParameterNameOfTypeNameBasedOnVersion(string wfsVersion)
        {
            return wfsVersion == "1.1.0" ? "typeName" : "typeNames";
        }
    }
}