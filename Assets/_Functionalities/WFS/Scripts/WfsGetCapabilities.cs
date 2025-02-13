using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wfs
{
    public class WfsGetCapabilities : BaseRequest, IGetCapabilities
    {
        public Uri GetCapabilitiesUri => Url;
        public const string DefaultFallbackVersion = "2.0.0"; // Default to 2.0.0 (released in 2010, compliant with ISO standards)

        public WfsGetCapabilities(Uri sourceUrl, string xml) : base(sourceUrl, xml)
        {
        }

        protected override Dictionary<string, string> defaultNameSpaces => new()
        {
            { "ows", "http://www.opengis.net/ows/1.1" },
            { "wfs", "http://www.opengis.net/wfs" },
            { "schemaLocation", "http://www.opengis.net/wfs" },
            { "fes", "http://www.opengis.net/fes/2.0" }
        };
        public ServiceType ServiceType => ServiceType.Wfs;

        public bool HasBounds //todo: this is suboptimal because it uses the GetBounds function, maybe cache the bounds
        {
            get
            {
                var bounds = GetBounds();
                if (bounds.GlobalBoundingBox == null && bounds.LayerBoundingBoxes.Count == 0)
                    return false;
                return true;
            }
        }

        public string GetVersion()
        {
            //try to get version from the url
            var urlLower = Url.ToString().ToLower();
            var versionQueryKey = "version=";
            if (urlLower.Contains(versionQueryKey))
                return urlLower.Split(versionQueryKey)[1].Split("&")[0];

            //try to get the version from the body, or return the default
            var versionInXml = xmlDocument.DocumentElement.GetAttribute("version");
            return !string.IsNullOrEmpty(versionInXml) ? versionInXml : DefaultFallbackVersion;
        }

        public string GetTitle() //todo: check if this can be made the same as wms
        {
            var title = xmlDocument?
                .DocumentElement?
                .SelectSingleNode("//*[local-name()='ServiceIdentification']/*[local-name()='Title']", namespaceManager);

            return title != null ? title.InnerText : "";
        }
        
        public List<string> GetLayerNames()
        {
            var layerNames = new List<string>();
    
            // Select all FeatureType nodes in the document
            var featureTypeNodes = xmlDocument.SelectNodes($"//*[local-name()='FeatureType']", namespaceManager);
    
            foreach (XmlNode featureTypeNode in featureTypeNodes)
            {
                var nameNode = featureTypeNode.SelectSingleNode("*[local-name()='Name']", namespaceManager);
                if (nameNode != null && !string.IsNullOrEmpty(nameNode.InnerText))
                {
                    layerNames.Add(nameNode.InnerText);
                }
            }

            return layerNames;
        }

        public BoundingBoxContainer GetBounds()
        {
            var boundingBoxContainer = new BoundingBoxContainer(Url.ToString());
            boundingBoxContainer.GlobalBoundingBox = GetGlobalBounds();
            
            foreach (var feature in GetFeatureTypes())
            {
                boundingBoxContainer.LayerBoundingBoxes.TryAdd(feature.Name, feature.BoundingBox);
            }

            return boundingBoxContainer;
        }

        public BoundingBox GetGlobalBounds()
        {
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
                    var globalBounds = new BoundingBox(bottomLeft, topRight);
                    return globalBounds;
                }
            }

            Debug.LogWarning("Global bounding box information not found in WFS GetCapabilities response.");
            return null;
        }

        private BoundingBox ReadFeatureBoundingBox(string featureTypeName)
        {
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


        public CoordinateSystem GetCoordinateReferenceSystem()
        {
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

        public bool WFSBboxFilterCapability()
        {
            if (GetVersion() != "2.0.0")
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

        private XmlNode ReadGetFeatureNode(XmlDocument xmlDocument, XmlNamespaceManager namespaceManager = null)
        {
            var getFeatureOperationNode = xmlDocument.SelectSingleNode("//ows:Operation[@name='GetFeature']", namespaceManager);

            if (getFeatureOperationNode == null)
            {
                Debug.LogWarning("<color=orange>WFS GetFeature operation not found.</color>");
            }

            return getFeatureOperationNode;
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

        private string GetGeoJSONOutputFormat(XmlNode xmlNode, XmlNamespaceManager namespaceManager = null)
        {
            var wfsVersion = GetVersion();

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

        public IEnumerable<FeatureType> GetFeatureTypes()
        {
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

        public static string GetLayerNameFromURL(string url)
        {
            var version = OgcWebServicesUtility.GetParameterFromURL(url, "version");
            var typeName = ParameterNameOfTypeNameBasedOnVersion(version);
            return OgcWebServicesUtility.GetParameterFromURL(url, typeName);
        }
        
        public static string ParameterNameOfTypeNameBasedOnVersion(string wfsVersion)
        {
            return wfsVersion == "1.1.0" ? "typeName" : "typeNames";
        }
    }
}