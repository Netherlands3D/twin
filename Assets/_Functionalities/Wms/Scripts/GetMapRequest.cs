using System;
using System.Collections.Specialized;
using System.Xml;
using Netherlands3D.Web;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.Wms
{
    public class GetMapRequest
    {
        private const string defaultFallbackVersion = "1.3.0"; // Default to 1.3.0 (?)
        private const string defaultCoordinateSystemReference = "EPSG:28992";

        private readonly Uri url;
        private readonly XmlDocument xmlDocument;
        private readonly XmlNamespaceManager namespaceManager;

        public static bool Supports(Uri url)
        {
            var queryString = url.Query.ToLower();

            return queryString.Contains("service=wms") && queryString.Contains("request=getmap");
        }
        
        public GetMapRequest(Uri sourceUrl, string cachedBodyFilePath)
        {
            this.url = sourceUrl;

            this.xmlDocument = new XmlDocument();
            this.xmlDocument.Load(cachedBodyFilePath);
            this.namespaceManager = CreateNameSpaceManager(this.xmlDocument);
        }

        internal Map CreateMapFromCapabilitiesUrl(int width, int height, bool transparent)
        {
            var version = GetParamValueFromSourceUrl(url.ToString(), "version");
            if (string.IsNullOrEmpty(version))
            {
                version = defaultFallbackVersion;
                Debug.LogWarning("WMS version could not be determined, defaulting to " + defaultFallbackVersion);
            }
            
            bool isHigherOrEqualVersion = Version.Parse(version) >= Version.Parse("1.3.0");

            var wmsParam = new Map
            {
                name = GetParamValueFromSourceUrl(url.ToString(), "layers"),
                spatialReferenceType = isHigherOrEqualVersion ? "CRS" : "SRS",
                spatialReference = defaultCoordinateSystemReference,
                style = GetParamValueFromSourceUrl(url.ToString(), "styles"),
                version = version,
                width = width,
                height = height,
                transparent = transparent
            };

            var crs = GetParamValueFromSourceUrl(url.ToString(), wmsParam.spatialReferenceType);
            wmsParam.spatialReference = !string.IsNullOrEmpty(crs) ? crs : defaultCoordinateSystemReference;

            return wmsParam;
        }

        private XmlNamespaceManager CreateNameSpaceManager(XmlDocument xmlDocument)
        {
            XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
            XmlNodeList elementsWithNamespaces = xmlDocument.SelectNodes("//*");
            namespaceManager.AddNamespace("wms", "http://www.opengis.net/wms");
            namespaceManager.AddNamespace("sld", "http://www.opengis.net/sld");
            namespaceManager.AddNamespace("ms", "http://mapserver.gis.umn.edu/mapserver");

            if (elementsWithNamespaces == null) return namespaceManager;

            foreach (XmlElement element in elementsWithNamespaces)
            {
                if (string.IsNullOrEmpty(element.NamespaceURI)) continue;
                
                string prefix = element.Name.Split(':')[0];
                if (string.IsNullOrEmpty(prefix) || namespaceManager.LookupNamespace(prefix) != null) continue;
                
                namespaceManager.AddNamespace(prefix, element.NamespaceURI);
            }

            return namespaceManager;
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