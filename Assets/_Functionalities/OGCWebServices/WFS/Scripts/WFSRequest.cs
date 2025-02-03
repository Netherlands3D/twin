using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Utility;
using Netherlands3D.Web;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wfs
{
    public class WFSRequest
    {
        private readonly string sourceUrl;
        private readonly string cachedBodyContent;

        private XmlDocument xmlDocument;
        private XmlNamespaceManager namespaceManager;

        public RequestType requestType;
        public BoundingBox wfsBounds;

        public enum RequestType
        {
            GetCapabilities,
            GetFeature,
            Unsupported
        }

        public WFSRequest(string sourceUrl, string xml)
        {
            this.sourceUrl = sourceUrl;
            this.cachedBodyContent = xml;
        }

        public void ParseBodyAsXML()
        {
            this.xmlDocument = new XmlDocument();
            this.xmlDocument.LoadXml(cachedBodyContent);
            this.namespaceManager = ReadNameSpaceManager(this.xmlDocument);
        }

        public bool IsGetCapabilitiesRequest()
        {
            // light weight -and rather ugly- check if this is a capabilities file without parsing the XML
            var couldBeWfsCapabilities = cachedBodyContent.Contains("<WFS_Capabilities") || cachedBodyContent.Contains("<wfs:WFS_Capabilities");

            var getCapabilitiesRequest = this.sourceUrl.ToLower().Contains("request=getcapabilities");
            requestType = RequestType.GetCapabilities;
            return getCapabilitiesRequest || couldBeWfsCapabilities;
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
            if (xmlDocument == null)
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
            if (xmlDocument == null)
                ParseBodyAsXML();

            var title = xmlDocument?
                .DocumentElement?
                .SelectSingleNode("//*[local-name()='ServiceIdentification']/*[local-name()='Title']", namespaceManager);

            return title != null ? title.InnerText : "";
        }

        public BoundingBox GetWFSBounds()
        {
            if (xmlDocument == null)
                ParseBodyAsXML();

            // Try to get a bounding box in the local CRS first
            var bboxNode = xmlDocument?.DocumentElement?
                .SelectSingleNode("//*[local-name()='boundedBy']/*[local-name()='Envelope']", namespaceManager);

            if (bboxNode != null)
            {
                var lowerCornerNode = bboxNode.SelectSingleNode("*[local-name()='lowerCorner']", namespaceManager);
                var upperCornerNode = bboxNode.SelectSingleNode("*[local-name()='upperCorner']", namespaceManager);
                var crsString = bboxNode.Attributes["srsName"]?.Value; // Use srsName instead of crs

                var hasCRS = CoordinateSystems.FindCoordinateSystem(crsString, out var crs);

                if (!hasCRS)
                {
                    crs = CoordinateSystem.CRS84; //default
                    Debug.LogWarning("Custom CRS BBox found, but not able to be parsed, defaulting to WGS84 CRS. Founds CRS string: " + crsString);
                }

                if (lowerCornerNode != null && upperCornerNode != null)
                {
                    var lowerCorner = lowerCornerNode.InnerText.Split(' ').Select(double.Parse).ToArray();
                    var upperCorner = upperCornerNode.InnerText.Split(' ').Select(double.Parse).ToArray();

                    Coordinate bottomLeft = new Coordinate(crs, lowerCorner);
                    Coordinate topRight = new Coordinate(crs, upperCorner);

                    Debug.Log($"Global Bounding box in CRS: {crs}");
                    wfsBounds = new BoundingBox(bottomLeft, topRight);
                    return wfsBounds;
                }
            }

            Debug.LogWarning("Global bounding box information not found in WFS GetCapabilities response.");
            return null;
        }

        public CoordinateSystem GetCoordinateReferenceSystem()
        {
            if (xmlDocument == null)
                ParseBodyAsXML();

            // Try to find the CRS in the FeatureType's DefaultCRS or DefaultSRS elements
            var crsNode = xmlDocument?.DocumentElement?
                .SelectSingleNode("//*[local-name()='FeatureTypeList']/*[local-name()='FeatureType']/*[local-name()='DefaultCRS' or local-name()='DefaultSRS']", namespaceManager);

            if (crsNode == null)
            {
                Debug.LogWarning("Coordinate Reference System (CRS) not found in the WFS GetCapabilities response.");
                return CoordinateSystem.Undefined;
            }

            var hasCRS = CoordinateSystems.FindCoordinateSystem(crsNode.InnerText, out var crs);
            if (hasCRS)
                return crs;

            Debug.LogWarning("Could not parse Coordinate Reference System (CRS) in the WFS GetCapabilities response. Founds CRS string: " + crsNode.InnerText);
            return CoordinateSystem.Undefined;
        }

        public IEnumerable<FeatureType> GetFeatureTypes()
        {
            if (xmlDocument == null)
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

            var schemaLocation = namespaceManager.LookupNamespace("schemaLocation");

            // Unsupported version
            if (string.IsNullOrEmpty(schemaLocation)) return featureTypes;

            XmlSerializer serializer = new XmlSerializer(
                typeof(FeatureType),
                new XmlRootAttribute("FeatureType")
                {
                    Namespace = schemaLocation
                }
            );
            foreach (XmlNode featureTypeNode in featureTypeChildNodes)
            {
                if (featureTypeNode.LocalName == "Operations") // or any other unwanted element
                {
                    continue;
                }

                var crsNode = featureTypeNode.SelectSingleNode("wfs:DefaultSRS | wfs:DefaultCRS", namespaceManager);
                string crs = crsNode?.InnerText;

                using (XmlNodeReader reader = new XmlNodeReader(featureTypeNode))
                {
                    reader.MoveToContent(); // Move to the root element of the node

                    // Deserialize the FeatureType element while handling the namespaces properly
                    FeatureType featureType = serializer.Deserialize(reader) as FeatureType;
                    if (featureType == null) continue;

                    featureType.BoundingBox = ReadFeatureBoundingBox(featureType.Name);
                    featureType.DefaultCRS = crs;
                    featureTypes.Add(featureType);
                }
            }

            return featureTypes;
        }

        private BoundingBox ReadFeatureBoundingBox(string featureTypeName)
        {
            if (xmlDocument == null)
                ParseBodyAsXML();

            var featureTypeNode = xmlDocument?.DocumentElement?
                .SelectSingleNode($"//*[local-name()='FeatureTypeList']/*[local-name()='FeatureType'][*[local-name()='Name']= '{featureTypeName}']", namespaceManager);

            if (featureTypeNode == null)
            {
                Debug.LogWarning($"FeatureType '{featureTypeName}' not found in WFS GetCapabilities response.");
                return null;
            }

            // Locate the WGS84BoundingBox for the selected FeatureType
            var wgs84BoundingBoxNode = featureTypeNode.SelectSingleNode("./*[local-name()='WGS84BoundingBox']", namespaceManager);

            if (wgs84BoundingBoxNode == null)
            {
                Debug.LogWarning($"Bounding box information not found for featureType '{featureTypeName}'.");
                return null;
            }

            // Extract lower and upper corners
            var wgs84LowerCornerNode = wgs84BoundingBoxNode.SelectSingleNode("./*[local-name()='LowerCorner']", namespaceManager);
            var wgs84UpperCornerNode = wgs84BoundingBoxNode.SelectSingleNode("./*[local-name()='UpperCorner']", namespaceManager);

            if (wgs84LowerCornerNode == null || wgs84UpperCornerNode == null)
            {
                Debug.LogWarning("Bounding box corners not found in WGS84BoundingBox node.");
                return null;
            }

            var lowerCornerValues = wgs84LowerCornerNode.InnerText.Split(' ').Select(double.Parse).ToArray();
            var upperCornerValues = wgs84UpperCornerNode.InnerText.Split(' ').Select(double.Parse).ToArray();

            var wgs84Crs = CoordinateSystem.CRS84; //WFS describes the WGS84BoundingBox as a lower and upper corner in x/y order, regardless of the DefaultCRS for some reason.

            var wgs84BottomLeft = new Coordinate(wgs84Crs, lowerCornerValues);
            var wgs84TopRight = new Coordinate(wgs84Crs, upperCornerValues);

            return new BoundingBox(wgs84BottomLeft, wgs84TopRight);
        }

        private XmlNamespaceManager ReadNameSpaceManager(XmlDocument xmlDocument)
        {
            XmlNamespaceManager namespaceManager = new(xmlDocument.NameTable);
            XmlElement rootElement = xmlDocument.DocumentElement;
            if (rootElement.HasAttribute("xmlns:ows"))
            {
                string owsNamespace = rootElement.GetAttribute("xmlns:ows");
                namespaceManager.AddNamespace("ows", owsNamespace);
            }
            else
            {
                Debug.Log("Adding ows namespace manually: http://www.opengis.net/ows/1.1");
                namespaceManager.AddNamespace("ows", "http://www.opengis.net/ows/1.1");
            }

            if (rootElement.HasAttribute("xmlns:wfs"))
            {
                string wfsNamespace = rootElement.GetAttribute("xmlns:wfs");
                namespaceManager.AddNamespace("wfs", wfsNamespace);
            }
            else
            {
                Debug.Log("Adding wfs namespace manually: http://www.opengis.net/wfs");
                namespaceManager.AddNamespace("wfs", "http://www.opengis.net/wfs");
            }

            if (rootElement.HasAttribute("xsi:schemaLocation"))
            {
                string schemaLocation = rootElement.GetAttribute("xsi:schemaLocation").Split(' ')[0];
                namespaceManager.AddNamespace("schemaLocation", schemaLocation);
            }
            else
            {
                Debug.Log("Adding schemaLocation namespace manually: http://www.opengis.net/wfs");
                namespaceManager.AddNamespace("schemaLocation", "http://www.opengis.net/wfs");
            }

            XmlNodeList elementsWithNamespaces = xmlDocument.SelectNodes("//*");
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
            {
                Debug.LogWarning("<color=orange>WFS GetFeature operation not found.</color>");
            }

            return getFeatureOperationNode;
        }
        
        public static string ParameterNameOfTypeNameBasedOnVersion(string wfsVersion)
        {
            return wfsVersion == "1.1.0" ? "typeName" : "typeNames";
        }

        public static string GetLayerNameFromURL(string url)
        {
            var uri = new Uri(url);
            var nvc = new NameValueCollection();
            uri.TryParseQueryString(nvc);
            var version = nvc.Get("version");
            var featureLayerName = nvc.Get(WFSRequest.ParameterNameOfTypeNameBasedOnVersion(version));
            return featureLayerName;
        }
    }
}