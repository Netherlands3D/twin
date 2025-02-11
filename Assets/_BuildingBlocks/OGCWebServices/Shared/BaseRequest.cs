using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Netherlands3D.OgcWebServices.Shared
{
    public enum ServiceType
    {
        Wfs,
        Wms
    }

    public enum RequestType
    {
        GetCapabilities,
        GetFeature,
        GetMap
    }

    public abstract class BaseRequest
    {
        protected const string defaultCoordinateSystemReference = "EPSG:28992";

        public readonly Uri Url;
        protected readonly XmlDocument xmlDocument;
        protected readonly XmlNamespaceManager namespaceManager;
        protected virtual Dictionary<string, string> defaultNameSpaces { get; } = new();

        protected BaseRequest(Uri sourceUrl, string xml)
        {
            Url = sourceUrl;
            xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            namespaceManager = CreateOrReadNamespaceManager(xmlDocument, defaultNameSpaces);
        }

        private XmlNamespaceManager CreateOrReadNamespaceManager(XmlDocument xmlDocument, Dictionary<string, string> defaultNamespaces)
        {
            XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
            XmlNodeList elementsWithNamespaces = xmlDocument.SelectNodes("//*");

            // Add namespaces provided in the dictionary
            foreach (var ns in defaultNamespaces)
            {
                namespaceManager.AddNamespace(ns.Key, ns.Value);
            }

            // If no elements with namespaces, we can return early
            if (elementsWithNamespaces == null) return namespaceManager;

            // Update namespaces if present in the XML document
            XmlElement rootElement = xmlDocument.DocumentElement;
            foreach (var ns in defaultNamespaces.Keys)
            {
                string attributeName = "xmlns:" + ns;
                if (rootElement.HasAttribute(attributeName))
                {
                    string foundNamespace = rootElement.GetAttribute(attributeName);
                    namespaceManager.AddNamespace(ns, foundNamespace);
                }
            }

            // Handle xsi:schemaLocation separately
            if (rootElement.HasAttribute("xsi:schemaLocation"))
            {
                string schemaLocation = rootElement.GetAttribute("xsi:schemaLocation").Split(' ')[0];
                namespaceManager.AddNamespace("schemaLocation", schemaLocation);
            }

            // Add any namespaces from the XML elements
            if (elementsWithNamespaces != null)
            {
                foreach (XmlElement element in elementsWithNamespaces)
                {
                    if (!string.IsNullOrEmpty(element.NamespaceURI))
                    {
                        string prefix = element.Name.Split(':')[0];
                        if (!string.IsNullOrEmpty(prefix) && namespaceManager.LookupNamespace(prefix) == null)
                        {
                            namespaceManager.AddNamespace(prefix, element.NamespaceURI);
                        }
                    }
                }
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