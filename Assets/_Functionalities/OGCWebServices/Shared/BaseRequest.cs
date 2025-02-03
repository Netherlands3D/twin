using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace Netherlands3D.Functionalities.OgcWebServices.Shared
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
        protected const string defaultFallbackVersion = "1.3.0";
        protected const string defaultCoordinateSystemReference = "EPSG:28992";

        protected readonly Uri url;
        protected readonly XmlDocument xmlDocument;
        protected readonly XmlNamespaceManager namespaceManager;
        protected abstract Dictionary<string, string> defaultNameSpaces { get; }

        protected BaseRequest(Uri sourceUrl, string xml)
        {
            url = sourceUrl;
            xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            namespaceManager = CreateOrReadNamespaceManager(xmlDocument, defaultNameSpaces);
        }

        // private XmlNamespaceManager CreateNameSpaceManager(XmlDocument xmlDocument)
        // {
        //     XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
        //     XmlNodeList elementsWithNamespaces = xmlDocument.SelectNodes("//*");
        //     namespaceManager.AddNamespace("wms", "http://www.opengis.net/wms");
        //     namespaceManager.AddNamespace("sld", "http://www.opengis.net/sld");
        //     namespaceManager.AddNamespace("ms", "http://mapserver.gis.umn.edu/mapserver");
        //
        //     if (elementsWithNamespaces == null) return namespaceManager;
        //
        //     foreach (XmlElement element in elementsWithNamespaces)
        //     {
        //         if (string.IsNullOrEmpty(element.NamespaceURI)) continue;
        //         
        //         string prefix = element.Name.Split(':')[0];
        //         if (string.IsNullOrEmpty(prefix) || namespaceManager.LookupNamespace(prefix) != null) continue;
        //         
        //         namespaceManager.AddNamespace(prefix, element.NamespaceURI);
        //     }
        //
        //     return namespaceManager;
        // }

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
            foreach (var ns in defaultNamespaces.Keys) // Use ToList to avoid modifying collection while iterating
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
            // if (rootElement.HasAttribute("xmlns:ows"))
            // {
            //     string owsNamespace = rootElement.GetAttribute("xmlns:ows");
            //     namespaceManager.AddNamespace("ows", owsNamespace);
            // }
            // else
            // {
            //     Debug.Log("Adding ows namespace manually: http://www.opengis.net/ows/1.1");
            //     namespaceManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");
            // }
            //
            // if (rootElement.HasAttribute("xmlns:wfs"))
            // {
            //     string wfsNamespace = rootElement.GetAttribute("xmlns:wfs");
            //     namespaceManager.AddNamespace("wfs", wfsNamespace);
            // }
            // else
            // {
            //     Debug.Log("Adding wfs namespace manually: http://www.opengis.net/wfs");
            //     namespaceManager.AddNamespace("wfs", "http://www.opengis.net/wfs");
            // }
            //
            // if (rootElement.HasAttribute("xsi:schemaLocation"))
            // {
            //     string schemaLocation = rootElement.GetAttribute("xsi:schemaLocation").Split(' ')[0];
            //     namespaceManager.AddNamespace("schemaLocation", schemaLocation);
            // }
            // else
            // {
            //     Debug.Log("Adding schemaLocation namespace manually: http://www.opengis.net/wfs");
            //     namespaceManager.AddNamespace("schemaLocation", "http://www.opengis.net/wfs");
            // }

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