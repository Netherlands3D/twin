using System.IO;
using System.Xml;
using UnityEngine;
using System;
using Netherlands3D.Web;
using System.Collections.Generic;
using System.Collections.Specialized;
using Netherlands3D.Twin.Layers;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/WFSImportAdapter", fileName = "WFSImportAdapter", order = 0)]
    public class WFSGeoJSONImportAdapter : ScriptableObject, IDataTypeAdapter
    {
        [SerializeField] private WFSGeoJsonLayerGameObject layerPrefab;

        private string wfsVersion = "";
        private const string defaultFallbackVersion = "2.0.0"; // Default to 2.0.0 (released in 2010, compliant with ISO standards)

        private GeoJSONWFS wfs;

        public bool Supports(LocalFile localFile)
        {
            var cachedDataPath = localFile.LocalFilePath;
            var sourceUrl = localFile.SourceUrl;

            if ((!sourceUrl.ToLower().Contains("service=wfs") && !sourceUrl.ToLower().Contains("/wfs"))
                || sourceUrl.ToLower().Contains("request=getmap")) //if request = getmap it means wms
                return false;

            Debug.Log("Checking source WFS url: " + sourceUrl);
            wfs = new GeoJSONWFS(sourceUrl, cachedDataPath);

            //If the body is a specific GetFeature request; directly continue to execute
            bool isGetFeatureRequest = wfs.IsGetFeatureRequest();
            if (isGetFeatureRequest)
                return true;

            //If the body is a GetCapabilities request; check if the WFS supports BBOX filter and GeoJSON output
            bool IsGetCapabilitiesRequest = wfs.IsGetCapabilitiesRequest();
            if(!IsGetCapabilitiesRequest)
                return false;

            wfs.ParseBodyAsXML();
            if (!wfs.HasBboxFilterCapability())
            {
                Debug.Log("<color=orange>WFS BBOX filter not supported.</color>");
                return false;
            }

            if (!wfs.HasGetFeatureAsGeoJSON())
            {
                Debug.Log("<color=orange>WFS GetFeature operation does not support GeoJSON output format.</color>");
                return false;
            }

            wfsVersion = wfs.GetWFSVersion();

            return true;
        }

        public void Execute(LocalFile localFile)
        {
            var sourceUrl = localFile.SourceUrl;
            var wfsFolder = new FolderLayer(sourceUrl);

            if (wfs.requestType == GeoJSONWFS.RequestType.GetCapabilities)
            {
                var featureTypes = wfs.GetFeatureTypes();

                //Create a folder layer 
                foreach (var featureType in featureTypes)
                {
                    Debug.Log("Adding WFS layer for featureType: " + featureType);
                    AddWFSLayer(featureType, sourceUrl, wfsFolder);
                }
                
                wfs = null;
                return;
            }

            if (wfs.requestType == GeoJSONWFS.RequestType.GetFeature)
            {
                // Get the feature type from the url
                var featureType = string.Empty;
                if (sourceUrl.ToLower().Contains("typename="))
                {
                    //WFS 1.0.0 uses 'typename'
                    featureType = sourceUrl.ToLower().Split("typename=")[1].Split("&")[0];
                }
                else if (sourceUrl.ToLower().Contains("typenames="))
                {
                    //WFS 2 uses plural 'typenames'
                    featureType = sourceUrl.ToLower().Split("typenames=")[1].Split("&")[0];
                }
                AddWFSLayer(featureType, sourceUrl, wfsFolder);

                wfs = null;
                return;
            }
        }

        private void AddWFSLayer(string featureType, string sourceUrl, FolderLayer folderLayer)
        {
            Debug.Log("Adding WFS layer: " + featureType);

            // Create a GetFeature URL for the specific featureType
            UriBuilder uriBuilder = CreateLayerUri(featureType, sourceUrl);
            var getFeatureUrl = uriBuilder.Uri.ToString();

            //Spawn a new WFS GeoJSON layer
            WFSGeoJsonLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.LayerData.SetParent(folderLayer);
            newLayer.Name = featureType;
            newLayer.SetURL(getFeatureUrl);
        }

        private UriBuilder CreateLayerUri(string featureType, string sourceUrl)
        {
            // Start by removing any query parameters we want to inject
            var uriBuilder = new UriBuilder(sourceUrl);

            // Make sure we have a wfsVersion set
            if (string.IsNullOrEmpty(wfsVersion))
            {
                Debug.LogWarning("WFS version could not be determined, defaulting to " + defaultFallbackVersion);
                wfsVersion = defaultFallbackVersion;
            }

            var parameters = new NameValueCollection();
            uriBuilder.TryParseQueryString(parameters);

            // Set the required query parameters for the GetFeature request
            uriBuilder.SetQueryParameter("service", "WFS");
            uriBuilder.SetQueryParameter("request", "GetFeature");
            uriBuilder.SetQueryParameter("version", wfsVersion);
            uriBuilder.SetQueryParameter("typeNames", featureType);
            if (parameters.Get("outputFormat")?.ToLower() is not ("json" or "geojson"))
            {
                uriBuilder.SetQueryParameter("outputFormat", "geojson");
            }
            uriBuilder.SetQueryParameter("bbox", "{0}"); // Bbox value is injected by CartesianTileWFSLayer
            return uriBuilder;
        }

        private class GeoJSONWFS
        {
            private readonly string sourceUrl;
            private readonly string cachedBodyContent;

            private XmlDocument xmlDocument;
            private XmlNamespaceManager namespaceManager;

            public RequestType requestType;
            public enum RequestType
            {
                GetCapabilities,
                GetFeature,
                Unsupported
            }

            public GeoJSONWFS(string sourceUrl, string cachedBodyFilePath)
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

            public bool IsGetFeatureRequest()
            {
                var getFeatureRequest = this.sourceUrl.ToLower().Contains("request=getfeature");
                requestType = RequestType.GetFeature;
                return getFeatureRequest;
            }

            public bool HasBboxFilterCapability()
            {
                return WFSBboxFilterCapability(this.xmlDocument, this.namespaceManager);
            }

            public bool HasGetFeatureAsGeoJSON()
            {
                XmlNode getFeatureOperationNode = ReadGetFeatureNode(this.xmlDocument, this.namespaceManager);
                if (getFeatureOperationNode == null)
                {
                    Debug.Log("<color=orange>WFS GetFeature operation not found.</color>");
                    return false;
                }

                var geoJsonOutFormat = GetGeoJSONOutputFormat(getFeatureOperationNode, namespaceManager);
                if (string.IsNullOrEmpty(geoJsonOutFormat))
                {
                    Debug.Log("<color=orange>WFS GetFeature operation does not support GeoJSON output format.</color>");
                    return false;
                }

                return true;
            }

            public string GetWFSVersion()
            {
                var urlLower = sourceUrl.ToLower();
                var versionQueryKey = "version=";
                if (urlLower.Contains(versionQueryKey))
                    return urlLower.Split(versionQueryKey)[1].Split("&")[0];

                return GetWFSVersionFromBody();
            }

            public string GetWFSVersionFromBody()
            {
                if(xmlDocument == null)
                    ParseBodyAsXML();

                var serviceTypeVersion = xmlDocument.SelectSingleNode("//ows:ServiceTypeVersion", namespaceManager);
                if (serviceTypeVersion != null)
                {
                    Debug.Log("WFS version found: " + serviceTypeVersion.InnerText);
                    return serviceTypeVersion.InnerText;
                }
                return "";
            }

            public string[] GetFeatureTypes()
            {
                if(xmlDocument == null)
                    ParseBodyAsXML();

                var featureTypeListNodeInRoot = xmlDocument.SelectSingleNode("//*[local-name()='FeatureTypeList']", namespaceManager);
                var featureTypeChildNodes = featureTypeListNodeInRoot.ChildNodes;
                var featureTypes = new List<string>();

                foreach (XmlNode featureTypeNode in featureTypeChildNodes)
                {
                    var featureTypeName = featureTypeNode.SelectSingleNode(".//*[local-name()='Name']", namespaceManager).InnerText;
                    featureTypes.Add(featureTypeName);
                }

                return featureTypes.ToArray();
            }

            private XmlNamespaceManager ReadNameSpaceManager(XmlDocument xmlDocument)
            {
                XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
                XmlNodeList elementsWithNamespaces = xmlDocument.SelectNodes("//*");
                namespaceManager.AddNamespace("wfs", "http://www.opengis.net/wfs");

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

            private bool WFSBboxFilterCapability(XmlDocument xmlDocument, XmlNamespaceManager namespaceManager = null)
            {
                var filterCapabilitiesNodeInRoot = xmlDocument.SelectSingleNode("//fes:SpatialOperators", namespaceManager);
                var bboxFilter = false;
                foreach (XmlNode spatialOperator in filterCapabilitiesNodeInRoot.ChildNodes)
                {
                    if (spatialOperator.Attributes["name"].Value.ToLower() == "bbox")
                    {
                        bboxFilter = true;
                    }
                }

                return bboxFilter;
            }

            private string GetGeoJSONOutputFormat(XmlNode xmlNode, XmlNamespaceManager namespaceManager = null)
            {
                var featureOutputFormat = xmlNode.SelectSingleNode("ows:Parameter[@name='outputFormat']", namespaceManager);
                var owsAllowedValues = featureOutputFormat.SelectSingleNode("ows:AllowedValues", namespaceManager);
                foreach (XmlNode owsValue in owsAllowedValues.ChildNodes)
                {
                    if (owsValue.InnerText.ToLower().Contains("geojson") || owsValue.InnerText.ToLower().Contains("json"))
                        return "geojson";
                }

                Debug.LogWarning("WFS GetFeature operation does not support GeoJSON output format.");
                return "";
            }

            private XmlNode ReadGetFeatureNode(XmlDocument xmlDocument, XmlNamespaceManager namespaceManager = null)
            {
                var getFeatureOperationNode = xmlDocument.SelectSingleNode("//ows:Operation[@name='GetFeature']", namespaceManager);

                if (getFeatureOperationNode == null)
                    Debug.LogWarning("WFS GetFeature operation not found.");

                return getFeatureOperationNode;
            }
        }
    }
}
