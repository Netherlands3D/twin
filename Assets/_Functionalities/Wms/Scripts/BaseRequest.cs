using System;
using System.Xml;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    public abstract class BaseRequest
    {
        internal const string defaultFallbackVersion = "1.3.0";
        internal const string defaultCoordinateSystemReference = "EPSG:28992";

        protected readonly Uri url;
        protected readonly XmlDocument xmlDocument;
        protected readonly XmlNamespaceManager namespaceManager;

        public BaseRequest(Uri sourceUrl, string cachedBodyFilePath)
        {
            url = sourceUrl;

            xmlDocument = new XmlDocument();
            xmlDocument.Load(cachedBodyFilePath);
            namespaceManager = CreateNameSpaceManager(this.xmlDocument);
        }

        protected static bool IsSupportedUrl(Uri url, string requestType)
        {
            var queryString = url.Query.ToLower();

            return queryString.Contains("service=wms") && queryString.Contains("request=" + requestType.ToLower());
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
        
        protected XmlNodeList GetMultipleNodesByName(XmlNode layerNode, string nodeName)
        {
            return layerNode.SelectNodes($".//*[local-name()='{nodeName}']", namespaceManager);
        }

        protected string GetInnerTextForNode(XmlNode layerNode, string nodeName, bool searchInParents = false)
        {
            try
            {
                return GetSingleNodeByName(layerNode, nodeName, searchInParents).InnerText;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get text for node {nodeName} in node {layerNode.InnerXml}: " + e.Message);
                return "";
            }
        }

        protected XmlNode GetSingleNodeByName(XmlNode layerNode, string nodeName, bool searchInParents = false)
        {
            // Base query that will attempt to find the node; but we need more ...
            var queryForNode = $"*[local-name()='{nodeName}']";
            
            if (searchInParents)
            {
                // ... when this flag is provided, check 'self' first and then traverse ancestors, or ...
                return layerNode.SelectSingleNode($"ancestor-or-self::*/child::{queryForNode}", namespaceManager);
            }

            // ... without the flag we limit the search to the layerNode for performance and to prevent unwanted hits
            return layerNode.SelectSingleNode($".//{queryForNode}", namespaceManager);
        }
    }
}