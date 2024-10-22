using System.IO;
using System.Xml;
using UnityEngine;
using System;
using Netherlands3D.Web;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml.Serialization;
using Netherlands3D.Twin.Layers;
using UnityEngine.Networking;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;

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

            Debug.Log("Checking source WFS url: " + sourceUrl);
            wfs = new GeoJSONWFS(sourceUrl, cachedDataPath);

            //If the body is a specific GetFeature request; directly continue to execute
            bool isGetFeatureRequest = wfs.IsGetFeatureRequest();
            if (isGetFeatureRequest)
                return true;

            //If the body is a GetCapabilities request; check if the WFS supports BBOX filter and GeoJSON output
            bool IsGetCapabilitiesRequest = wfs.IsGetCapabilitiesRequest();
            if (!IsGetCapabilitiesRequest) {
                Debug.Log("<color=orange>WFS: No GetFeature nor GetCapabilities request type found.</color>");
                return false;
            }

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
            var wfsFolder = new FolderLayer(!string.IsNullOrEmpty(wfs.GetTitle()) ? wfs.GetTitle() : sourceUrl);

            switch (wfs.requestType)
            {
                case GeoJSONWFS.RequestType.GetCapabilities:
                {
                    var featureTypes = wfs.GetFeatureTypes();

                    //Create a folder layer 
                    foreach (var featureType in featureTypes)
                    {
                        Debug.Log("Adding WFS layer for featureType: " + featureType);
                        AddWFSLayer(featureType.Name, sourceUrl, wfsFolder, featureType.Title);
                    }
                
                    wfs = null;
                    return;
                }
                case GeoJSONWFS.RequestType.GetFeature:
                {
                    NameValueCollection queryParameters = new();
                    new Uri(sourceUrl).TryParseQueryString(queryParameters);
                    var featureType = queryParameters.Get(ParameterNameOfTypeNameBasedOnVersion());

                    if (string.IsNullOrEmpty(featureType) == false)
                    {
                        // Can't deduct a human-readable title at the moment, we should add that we always query for the
                        // capabilities; this also helps with things like outputFormat and CRS
                        AddWFSLayer(featureType, sourceUrl, wfsFolder, featureType);
                    }

                    wfs = null;
                    return;
                }
                default:
                    Debug.LogError("Unrecognized WFS request type: " + wfs.requestType);
                    break;
            }
        }

        private void AddWFSLayer(string featureType, string sourceUrl, FolderLayer folderLayer, string title)
        {
            // Create a GetFeature URL for the specific featureType
            UriBuilder uriBuilder = CreateLayerUri(featureType, sourceUrl);
            var getFeatureUrl = uriBuilder.Uri.ToString();

            Debug.Log($"Adding WFS layer '{featureType}' with url '{getFeatureUrl}'");

            //Spawn a new WFS GeoJSON layer
            WFSGeoJsonLayerGameObject newLayer = Instantiate(layerPrefab);
            newLayer.LayerData.SetParent(folderLayer);
            newLayer.Name = title;

            var propertyData = newLayer.PropertyData as LayerURLPropertyData;
            propertyData.Data = AssetUriFactory.CreateRemoteAssetUri(getFeatureUrl); 
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
            uriBuilder.SetQueryParameter(ParameterNameOfTypeNameBasedOnVersion(), featureType);
            if (parameters.Get("outputFormat")?.ToLower() is not ("json" or "geojson"))
            {
                var geoJsonOutputFormatString = wfs.GetGeoJsonOutputFormatString();
                uriBuilder.SetQueryParameter(
                    "outputFormat", 
                    !string.IsNullOrEmpty(geoJsonOutputFormatString) ? geoJsonOutputFormatString : "application/json"
                );
            }
            uriBuilder.SetQueryParameter("bbox", "{0}"); // Bbox value is injected by CartesianTileWFSLayer

            return uriBuilder;
        }

        private string ParameterNameOfTypeNameBasedOnVersion()
        {
            return wfsVersion == "1.1.0" ? "typeName" : "typeNames";
        }

        [Serializable]
        public class FeatureType
        {
            public string Name;
            public string Title;
            public string Abstract;
            public string DefaultCRS;
            public string[] OtherCRS;
            public string MetadataURL;
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

            public string GetGeoJsonOutputFormatString()
            {
                XmlNode getFeatureOperationNode = ReadGetFeatureNode(this.xmlDocument, this.namespaceManager);
                if (getFeatureOperationNode == null)
                {
                    Debug.Log("<color=orange>WFS GetFeature operation not found.</color>");
                    return "";
                }

                return GetGeoJSONOutputFormat(getFeatureOperationNode, namespaceManager);
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

                var serviceTypeVersion = xmlDocument.DocumentElement.Attributes["version"];
                if (serviceTypeVersion != null)
                {
                    Debug.Log("WFS version found: " + serviceTypeVersion.InnerText);
                    return serviceTypeVersion.InnerText;
                }

                return "";
            }

            public string GetTitle()
            {
                if(xmlDocument == null)
                    ParseBodyAsXML();

                var title = xmlDocument?
                    .DocumentElement?
                    .SelectSingleNode("//*[local-name()='ServiceIdentification']/*[local-name()='Title']", namespaceManager);
                
                return title != null ? title.InnerText : "";
            }

            public IEnumerable<FeatureType> GetFeatureTypes()
            {
                if(xmlDocument == null)
                    ParseBodyAsXML();

                var featureTypeListNodeInRoot = xmlDocument?.DocumentElement?
                    .SelectSingleNode("//*[local-name()='FeatureTypeList']", namespaceManager);
                var featureTypeChildNodes = featureTypeListNodeInRoot?.ChildNodes;
                var featureTypes = new List<FeatureType>();
                if (featureTypeChildNodes == null)
                {
                    Debug.LogWarning("No feature types were found in WFS' GetCapabilities response");
                    return featureTypes;
                }

                var wfsVersion = GetWFSVersion();
                string namespaceVersion = wfsVersion switch
                {
                    "1.1.0" => "1.1.0",
                    "2.0.0" => "2.0",
                    _ => null
                };

                // Unsupported version
                if (namespaceVersion == null) return featureTypes;

                XmlSerializer serializer = new XmlSerializer(
                    typeof(FeatureType), 
                    new XmlRootAttribute("FeatureType")
                    {
                        Namespace = "http://www.opengis.net/wfs/" + namespaceVersion
                    }
                );
                foreach (XmlNode featureTypeNode in featureTypeChildNodes)
                {
                    using XmlNodeReader reader = new XmlNodeReader(featureTypeNode);
                    
                    FeatureType featureType = serializer.Deserialize(reader) as FeatureType;
                    if (featureType == null) continue;
                        
                    featureTypes.Add(featureType);
                }

                return featureTypes;
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
                if (GetWFSVersion() != "2.0.0")
                {
                    // Let's guess it does, WFS prior to 2.0.0 do not report this
                    return true;
                }

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
                var wfsVersion = GetWFSVersion();

                var featureOutputFormat = xmlNode.SelectSingleNode(
                    "ows:Parameter[@name='outputFormat']", 
                    namespaceManager
                );

                var owsAllowedValues = wfsVersion switch
                {
                    "2.0.0" => featureOutputFormat?.SelectSingleNode("ows:AllowedValues", namespaceManager),
                    "1.1.0" => featureOutputFormat,
                    _ => null
                };

                if (owsAllowedValues == null)
                {
                    Debug.LogWarning("WFS GetFeature operation does not expose which output formats are supported.");
                    return "";
                }

                string outputString = "";
                foreach (XmlNode owsValue in owsAllowedValues.ChildNodes)
                {
                    var value = owsValue.InnerText;
                    var lowerCaseValue = value.ToLower();
                    
                    // Immediately return outputFormats containing the word 'geojson'; this is by definition the
                    // most specific and best option
                    if (lowerCaseValue.Contains("geojson"))
                    {
                        return value; // _Return_ complete string, in case it has a variation
                    }

                    // if there is no outputFormat with the term 'geojson' in it, let's store any format with the
                    // term json in it; this is _usually_ geojson
                    if (lowerCaseValue.Contains("json"))
                    {
                        outputString = value; // _Remember_ complete string, in case it has a variation
                    }
                }

                if (string.IsNullOrEmpty(outputString))
                {
                    Debug.LogWarning("WFS GetFeature operation does not support GeoJSON output format.");
                }

                return outputString;
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
