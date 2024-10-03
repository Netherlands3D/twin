using System.IO;
using System.Xml;
using UnityEngine;
using System;
using Netherlands3D.Web;
using System.Collections.Generic;
using System.Collections.Specialized;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Geoservice;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WMSImportAdapter", fileName = "WMSImportAdapter", order = 0)]
    public class WMSImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private WMSLayerGameObject layerPrefab;

        private string wmsVersion = "";
        private const string defaultFallbackVersion = "1.3.0"; // Default to 1.3.0 (?)

        private WMS wms;

        public bool Supports(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var sourceUrl = localFile.SourceUrl;

            if (!sourceUrl.ToLower().Contains("service=wms"))
                return false;

            Debug.Log("Checking source WMS url: " + sourceUrl);
            wms = new WMS(sourceUrl, cachedDataPath);

            //If the body is a GetCapabilities request; check if the WMS supports BBOX filter
            bool IsGetCapabilitiesRequest = wms.IsGetCapabilitiesRequest();
            if(!IsGetCapabilitiesRequest)
                return false;

            wms.ParseBodyAsXML();
            if (!wms.HasBboxFilterCapability())
            {
                Debug.Log("<color=orange>WMS BBOX filter not supported.</color>");
                return false;
            }           
            wmsVersion = wms.GetWMSVersion();

            return true;
        }

        public void Execute(LocalFile localFile)
        {
            var sourceUrl = localFile.SourceUrl;
            var wmsFolder = new FolderLayer(sourceUrl);

            if (wms.requestType == WMS.RequestType.GetCapabilities)
            {
                var layerTypes = wms.GetWmsLayers();

                //Create a folder layer 
                foreach (var layerType in layerTypes)
                {
                    Debug.Log("Adding WMS layer for layerType: " + layerType);
                    AddWMSLayer(layerType, sourceUrl, wmsFolder);
                }
                
                wms = null;
                return;
            }            
        }

        private void AddWMSLayer(string featureType, string sourceUrl, FolderLayer folderLayer)
        {
            Debug.Log("Adding WMS layer: " + featureType);

            // Create a GetFeature URL for the specific featureType
            UriBuilder uriBuilder = CreateLayerUri(featureType, sourceUrl);
            var getFeatureUrl = uriBuilder.Uri.ToString();

            //Spawn a new WFS GeoJSON layer
            WMSLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.LayerData.SetParent(folderLayer);
            newLayer.Name = featureType;
            newLayer.SetURL(getFeatureUrl);
        }

        private UriBuilder CreateLayerUri(string layerType, string sourceUrl)
        {
            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(sourceUrl);

            // Make sure we have a wfsVersion set
            if (string.IsNullOrEmpty(wmsVersion))
            {
                Debug.LogWarning("WMS version could not be determined, defaulting to " + defaultFallbackVersion);
                wmsVersion = defaultFallbackVersion;
            }

            var parameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(parameters);

            // Set the required query parameters for the GetFeature request
            uriBuilder.SetQueryParameter("service", "WFS");
            uriBuilder.SetQueryParameter("request", "GetMap");
            uriBuilder.SetQueryParameter("version", wmsVersion);
            uriBuilder.SetQueryParameter("layers", layerType);
            if (parameters.Get("format")?.ToLower() is not ("png" or "jpeg"))
            {
                uriBuilder.SetQueryParameter("format", "png");
            }
            uriBuilder.SetQueryParameter("bbox", "{0}"); // Bbox value is injected by ImageProjectionLayer
            return uriBuilder;
        }

        private class WMS
        {
            private readonly string sourceUrl;
            private readonly string cachedBodyContent;

            private XmlDocument xmlDocument;
            private XmlNamespaceManager namespaceManager;

            public RequestType requestType;
            public enum RequestType
            {
                GetCapabilities,
                Unsupported
            }

            public WMS(string sourceUrl, string cachedBodyFilePath)
            {
                this.sourceUrl = sourceUrl;
                this.cachedBodyContent = cachedBodyFilePath;
            }

            public void ParseBodyAsXML()
            {
                this.xmlDocument = new XmlDocument();
                this.xmlDocument.Load(this.cachedBodyContent);
                this.namespaceManager = ReadNameSpaceManager(this.xmlDocument);
            }

            public bool IsGetCapabilitiesRequest()
            {
                var getCapabilitiesRequest = this.sourceUrl.ToLower().Contains("request=getcapabilities");
                requestType = RequestType.GetCapabilities;
                return getCapabilitiesRequest;
            }

            public bool HasBboxFilterCapability()
            {
                return WMSBboxFilterCapability(this.xmlDocument, this.namespaceManager);
            }

            public string GetWMSVersion()
            {
                if (xmlDocument == null)
                    ParseBodyAsXML();

                // Use XPath to select the root node and get the version attribute
                var rootNode = xmlDocument.SelectSingleNode("/*");

                // Check if the root node is found and retrieve the version attribute
                if (rootNode != null && rootNode.Attributes != null)
                {
                    var versionAttribute = rootNode.Attributes["version"];
                    return versionAttribute?.Value; // Return the version value or null if not found
                }

                return null; // Return null if root node or version attribute is not found
            }

            public string GetWMSVersionFromBody()
            {
                if(xmlDocument == null)
                    ParseBodyAsXML();

                var serviceTypeVersion = xmlDocument.SelectSingleNode("//ows:ServiceTypeVersion", namespaceManager);
                if (serviceTypeVersion != null)
                {
                    Debug.Log("WMS version found: " + serviceTypeVersion.InnerText);
                    return serviceTypeVersion.InnerText;
                }
                return "";
            }

            public string[] GetWmsLayers()
            {
                if (xmlDocument == null)
                    ParseBodyAsXML(); // Parse the XML if not already done

                // Select the Layer nodes from the WMS capabilities document
                var capabilityNode = xmlDocument.SelectSingleNode("//*[local-name()='Capability']", namespaceManager);
                var layerNodes = capabilityNode.SelectNodes(".//*[local-name()='Layer']/*[local-name()='Layer']", namespaceManager);
                var layers = new List<string>();

                // Loop through the Layer nodes and get their names
                foreach (XmlNode layerNode in layerNodes)
                {
                    // Extract the Name node for each layer
                    var layerNameNode = layerNode.SelectSingleNode(".//*[local-name()='Name']", namespaceManager);
                    if (layerNameNode != null)
                    {
                        var layerName = layerNameNode.InnerText;
                        layers.Add(layerName);
                    }
                }

                // Return the list of layer names as an array
                return layers.ToArray();
            }

            private XmlNamespaceManager ReadNameSpaceManager(XmlDocument xmlDocument)
            {
                XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
                XmlNodeList elementsWithNamespaces = xmlDocument.SelectNodes("//*");
                namespaceManager.AddNamespace("wms", "http://www.opengis.net/wms");
                namespaceManager.AddNamespace("sld", "http://www.opengis.net/sld");
                namespaceManager.AddNamespace("ms", "http://mapserver.gis.umn.edu/mapserver");

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

            private bool WMSBboxFilterCapability(XmlDocument xmlDocument, XmlNamespaceManager namespaceManager = null)
            {
                // Select all BoundingBox nodes in the document
                var boundingBoxNodes = xmlDocument.SelectNodes("//*[local-name()='EX_GeographicBoundingBox']", namespaceManager);

                // Initialize bboxFilter to false
                bool bboxFilter = false;

                // Loop through each BoundingBox node to check if it exists
                if (boundingBoxNodes != null && boundingBoxNodes.Count > 0)
                {
                    bboxFilter = true; // Set to true if any BoundingBox nodes exist
                }
                return bboxFilter;
            }  
        }
    }
}
