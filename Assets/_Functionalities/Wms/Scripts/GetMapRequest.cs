using System;
using System.Xml;
using KindMen.Uxios;
using UnityEngine;

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

        internal MapFilters CreateMapFromCapabilitiesUrl(int width, int height, bool transparent)
        {
            var version = GetParamValueFromSourceUrl(url, "version");
            if (string.IsNullOrEmpty(version))
            {
                version = defaultFallbackVersion;
                Debug.LogWarning("WMS version could not be determined, defaulting to " + defaultFallbackVersion);
            }
            
            var wmsParam = new MapFilters
            {
                name = GetParamValueFromSourceUrl(url, "layers"),
                spatialReferenceType = SpatialReferenceTypeFromVersion(new Version(version)),
                spatialReference = defaultCoordinateSystemReference,
                style = GetParamValueFromSourceUrl(url, "styles"),
                version = version,
                width = width,
                height = height,
                transparent = transparent
            };

            var crs = GetParamValueFromSourceUrl(url, wmsParam.spatialReferenceType);
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

        public static string SpatialReferenceTypeFromVersion(Version version)
        {
            return version.CompareTo(new Version("1.3.0")) >= 0 ? "CRS" : "SRS";
        }


        private static string GetParamValueFromSourceUrl(Uri sourceUrl, string param)
        {
            var queryParameters = QueryString.Decode(sourceUrl.Query);

            return queryParameters.Get(param) ?? queryParameters.Get(param.ToLower());
        }
    }
}