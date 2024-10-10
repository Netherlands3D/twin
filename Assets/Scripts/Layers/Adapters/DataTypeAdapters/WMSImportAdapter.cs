using System.Xml;
using UnityEngine;
using System;
using Netherlands3D.Web;
using System.Collections.Generic;
using System.Collections.Specialized;
using Netherlands3D.Twin.Layers;

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
            string url = sourceUrl.Split('?')[0];
            var wmsFolder = new FolderLayer(url);

            if (wms.requestType == WMS.RequestType.GetCapabilities)
            {
                List<WMS.WMSLayerQueryParams> layerTypes = wms.GetWmsLayers();

                //Create a folder layer 
                for(int i = 0; i < layerTypes.Count; i++)
                {
                    AddWMSLayer(layerTypes[i], url, wmsFolder, i < layerPrefab.DefaultEnabledLayersMax);
                }
                
                wms = null;
                return;
            }            
        }

        private void AddWMSLayer(WMS.WMSLayerQueryParams layer, string sourceUrl, FolderLayer folderLayer, bool defaultEnabled)
        {
            //Spawn a new WMS layer
            WMSLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.LayerData.SetParent(folderLayer);
            newLayer.Name = layer.name;            

            Debug.Log("Adding WMS layer: " + layer.name);
            // Create a layerType URL for the specific layerType
            UriBuilder uriBuilder = CreateLayerUri(layer, sourceUrl);
            var getLayerTypeUrl = uriBuilder.Uri.ToString();
            string finalUrl = Uri.UnescapeDataString(getLayerTypeUrl);            
            
            newLayer.SetURL(finalUrl);
            newLayer.LayerData.ActiveSelf = defaultEnabled;
        }

        private UriBuilder CreateLayerUri(WMS.WMSLayerQueryParams layer, string sourceUrl)
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

            string encoded = Uri.EscapeDataString(layer.name);

            // Set the required query parameters for the GetFeature request
            uriBuilder.AddQueryParameter("service", "WMS");
            uriBuilder.AddQueryParameter("version", wmsVersion);
            uriBuilder.AddQueryParameter("request", "GetMap");
            uriBuilder.AddQueryParameter("layers", encoded);
            uriBuilder.AddQueryParameter("styles", layer.style);
            uriBuilder.AddQueryParameter(layer.spatialReferenceType, layer.spatialReference);
            uriBuilder.AddQueryParameter("bbox", "{0}"); // Bbox value is injected by ImageProjectionLayer
            uriBuilder.AddQueryParameter("width", layerPrefab.PreferredImageSize.x.ToString());
            uriBuilder.AddQueryParameter("height", layerPrefab.PreferredImageSize.y.ToString()); 
            if (parameters.Get("format")?.ToLower() is not ("image/png" or "image/jpeg"))
            {
                uriBuilder.AddQueryParameter("format", "image/png");
            }
            if (!sourceUrl.Contains("transparent="))
                uriBuilder.AddQueryParameter("transparent", layerPrefab.TransparencyEnabled.ToString());
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

            public struct WMSLayerQueryParams
            {
                public string name;
                public string spatialReferenceType;
                public string spatialReference;
                public string style;
            }

            public List<WMSLayerQueryParams> GetWmsLayers()
            {
                if (xmlDocument == null)
                    ParseBodyAsXML(); // Parse the XML if not already done

                // Select the Layer nodes from the WMS capabilities document
                var capabilityNode = xmlDocument.SelectSingleNode("//*[local-name()='Capability']", namespaceManager);
                var layerNodes = capabilityNode.SelectNodes(".//*[local-name()='Layer']/*[local-name()='Layer']", namespaceManager);                
                List<WMSLayerQueryParams> layers = new List<WMSLayerQueryParams>();
                // Loop through the Layer nodes and get their names
                foreach (XmlNode layerNode in layerNodes)
                {
                    WMSLayerQueryParams layerQueryParams = new WMSLayerQueryParams();
                    // Extract the Name node for each layer
                    var layerNameNode = layerNode.SelectSingleNode(".//*[local-name()='Name']", namespaceManager);
                    var srsNode = layerNode.SelectSingleNode(".//*[local-name()='SRS']", namespaceManager);
                    var crsNode = layerNode.SelectSingleNode(".//*[local-name()='CRS']", namespaceManager);
                    if (layerNameNode != null)
                    {
                        layerQueryParams.name = layerNameNode.InnerText;
                        if (crsNode != null)
                        {
                            layerQueryParams.spatialReferenceType = "CRS";
                            layerQueryParams.spatialReference = crsNode.InnerText;
                        }
                        else if (srsNode != null)
                        {
                            layerQueryParams.spatialReferenceType = "SRS";
                            layerQueryParams.spatialReference = srsNode.InnerText;
                        }

                        // Extract styles for the layer
                        var styleNodes = layerNode.SelectNodes(".//*[local-name()='Style']", namespaceManager);
                        var styles = new List<string>();

                        foreach (XmlNode styleNode in styleNodes)
                        {
                            var styleNameNode = styleNode.SelectSingleNode(".//*[local-name()='Name']", namespaceManager);
                            if (styleNameNode != null)
                            {
                                styles.Add(styleNameNode.InnerText);
                            }
                        }

                        // Add all styles, but pick the first one by default for the WMS request
                        if (styles.Count > 0)
                            layerQueryParams.style = styles[0]; // Pick the first style by default
                        else
                            layerQueryParams.style = "";                        
                        //layerQueryParams.style = string.Join(",", styles);

                    }
                    layers.Add(layerQueryParams);
                }
                // Return the list of layer names as an array
                return layers;
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
                boundingBoxNodes = xmlDocument.SelectNodes("//*[local-name()='BoundingBox']", namespaceManager);
                if (boundingBoxNodes != null && boundingBoxNodes.Count > 0)
                {
                    bboxFilter = true; // Set to true if any BoundingBox nodes exist
                }
                return bboxFilter;
            }  
        }
    }
}
