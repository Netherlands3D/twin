using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Netherlands3D.Coordinates;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    public class WmsGetCapabilities : BaseRequest, IGetCapabilities
    {
        public Uri GetCapabilitiesUri => Url;
        public const string DefaultFallbackVersion = "1.3.0";
        private CoordinateSystem[] preferredCRS = { CoordinateSystem.RD, CoordinateSystem.WGS84_LatLon, CoordinateSystem.CRS84 };

        public ServiceType ServiceType => ServiceType.Wms;
        protected override Dictionary<string, string> defaultNameSpaces => new()
        {
            { "ows", "http://www.opengis.net/ows/1.1" },
            { "wms", "http://www.opengis.net/wms" },
            { "sld", "http://www.opengis.net/sld" },
            { "ms", "http://mapserver.gis.umn.edu/mapserver" },
            { "schemaLocation", "http://www.opengis.net/wms" }
        };

        private Dictionary<CoordinateSystem, string> supportedCrsDictionary = new Dictionary<CoordinateSystem, string>();

        public bool CapableOfBoundingBoxes => xmlDocument.SelectSingleNode("//*[local-name()='EX_GeographicBoundingBox' or local-name()='BoundingBox']", namespaceManager) != null;

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

        public WmsGetCapabilities(Uri url, string xml) : base(url, xml)
        {
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

        public string GetTitle()
        {
            return GetInnerTextForNode(xmlDocument.DocumentElement, "Title");
        }

        public List<string> GetLayerNames()
        {
            List<string> layerNames = new List<string>();
            var LayerNodes = xmlDocument.SelectNodes("//*[local-name()='Layer']", namespaceManager);
            foreach (XmlNode layerNode in LayerNodes)
            {
                var nameNode = layerNode.SelectSingleNode("*[local-name()='Name']", namespaceManager);
                if (nameNode != null && !string.IsNullOrEmpty(nameNode.InnerText))
                {
                    layerNames.Add(nameNode.InnerText);
                }
            }

            return layerNames;
        }

        public BoundingBoxContainer GetBounds()
        {
            var container = new BoundingBoxContainer(Url.ToString());

            // Select EX_GeographicBoundingBox node first
            var bboxNode = xmlDocument.SelectSingleNode("//*[local-name()='EX_GeographicBoundingBox']", namespaceManager);
            if (bboxNode != null)
            {
                container.GlobalBoundingBox = ParseBoundingBox(bboxNode, CoordinateSystem.CRS84);
            }

            // Select BoundingBox nodes per layer
            var LayerNodes = xmlDocument.SelectNodes("//*[local-name()='Layer']", namespaceManager);
            foreach (XmlNode layerNode in LayerNodes)
            {
                string layerName = layerNode.SelectSingleNode("*[local-name()='Name']", namespaceManager)?.InnerText;
                if (string.IsNullOrEmpty(layerName)) continue;

                // We prefer EPSG:28992 because it matches our application better ..
                var boundingBoxNode = layerNode.SelectSingleNode("*[local-name()='BoundingBox' and @CRS='EPSG:28992']", namespaceManager);
                // .. Ok, WGS84 is Okay too ..  
                boundingBoxNode ??= layerNode.SelectSingleNode("*[local-name()='BoundingBox' and @CRS='EPSG:3857']", namespaceManager);
                // .. Seriously? That one isn't there either? Ok, let's pick the first and see what gives
                boundingBoxNode ??= layerNode.SelectSingleNode("*[local-name()='BoundingBox']", namespaceManager);
                
                // Wait what? Nothing?
                if (boundingBoxNode == null) continue;
                
                string crsString = boundingBoxNode.Attributes["CRS"].Value;
                // A hack used to aid identifying the correct CRS, the crsString that our coordinate converter
                // looks for is CRS84 and not CRS:84. The latter is the actual designation. Please see Annex B of
                // the WMS specification https://portal.ogc.org/files/?artifact_id=14416 to see more information
                // on the use of the CRS namespace -hence the colon between CRS and 84.
                if (crsString == "CRS:84") crsString = "CRS84";

                var hasCRS = CoordinateSystems.FindCoordinateSystem(crsString, out var crs);
                if (!hasCRS)
                {
                    crs = CoordinateSystem.CRS84; //default
                    Debug.LogWarning("CRS for BBox could not be recognized, defaulting to CRS:84. Found string: " + crsString);
                }

                container.LayerBoundingBoxes[layerName] = ParseBoundingBox(boundingBoxNode, crs);
            }

            return container;
        }

        private BoundingBox ParseBoundingBox(XmlNode node, CoordinateSystem crs)
        {
            if (node == null)
                return null;

            // This method seems to be reused in reading the EX_GeographicBoundingBox -which is always in CRS:84 but I
            // hope the caller does this correct-
            var minXString = node.SelectSingleNode("*[local-name()='westBoundLongitude']", namespaceManager)?.InnerText;
            var minYString = node.SelectSingleNode("*[local-name()='southBoundLatitude']", namespaceManager)?.InnerText;
            var maxXString = node.SelectSingleNode("*[local-name()='eastBoundLongitude']", namespaceManager)?.InnerText;
            var maxYString = node.SelectSingleNode("*[local-name()='northBoundLatitude']", namespaceManager)?.InnerText;

            // Ugly solution to support both EX_GeographicBoundingBox and BoundingBox, the latter uses attributes
            minXString ??= node.Attributes["minx"]?.Value;
            minYString ??= node.Attributes["miny"]?.Value;
            maxXString ??= node.Attributes["maxx"]?.Value;
            maxYString ??= node.Attributes["maxy"]?.Value;

            // replace , with . to ensure the parse function works as intended because some Dutch agencies use the wrong
            // decimal separators.
            minXString = minXString?.Replace(',', '.');
            minYString = minYString?.Replace(',', '.');
            maxXString = maxXString?.Replace(',', '.');
            maxYString = maxYString?.Replace(',', '.');
           
            if (!double.TryParse(minXString, out var minX))
                return null;
            if (!double.TryParse(minYString, out var minY))
                return null;
            if (!double.TryParse(maxXString, out var maxX))
                return null;
            if (!double.TryParse(maxYString, out var maxY))
                return null;

            var bl = new Coordinate(crs, minX, minY);
            var tr = new Coordinate(crs, maxX, maxY);

            return new BoundingBox(bl, tr);
        }

        public List<MapFilters> GetMaps(int width, int height, bool transparent)
        {
            // Select the Layer nodes from the WMS capabilities document
            var capabilityNode = GetSingleNodeByName(xmlDocument, "Capability");
            var mapNodes = capabilityNode.SelectNodes(".//*[local-name()='Layer']/*[local-name()='Layer']", namespaceManager);

            // Create a template that we can use as a basis for individual layers
            var mapTemplate = CreateMapTemplate(width, height, transparent);

            var maps = new List<MapFilters>();

            // Loop through the Layer nodes and get their names
            foreach (XmlNode mapNode in mapNodes)
            {
                // Extract the Name node for each layer
                var layerNameNode = GetInnerTextForNode(mapNode, "Name");
                if (string.IsNullOrEmpty(layerNameNode)) continue;

                // Extract styles for the layer
                var styles = ExtractStyles(mapNode);

                string spatialReference = null;
                var spatialReferenceType = MapFilters.SpatialReferenceTypeFromVersion(new Version(mapTemplate.version));
                XmlNodeList crsNodes = GetNodesByName(mapNode, spatialReferenceType);
                if (crsNodes.Count == 0)
                    crsNodes = GetNodesByNameAndAttributes(mapNode, spatialReferenceType);

                HasSupportedCRS(crsNodes, out spatialReference);
                if (string.IsNullOrEmpty(spatialReference))
                {
                    Debug.LogError("there is no CRS/SRS defined in the xml for this layer");
                    continue;
                }

                var map = new MapFilters()
                {
                    name = layerNameNode,
                    version = mapTemplate.version,
                    width = mapTemplate.width,
                    height = mapTemplate.height,
                    transparent = mapTemplate.transparent,
                    spatialReferenceType = mapTemplate.spatialReferenceType,
                    spatialReference = spatialReference,
                    style = styles.FirstOrDefault()
                };
                maps.Add(map);
            }

            // Return the list of layer names as an array
            return maps;
        }

        private bool HasSupportedCRS(XmlNodeList crsNodes, out string crsToUse)
        {
            supportedCrsDictionary.Clear();

            //parse all available crs nodes
            foreach (XmlNode crsNode in crsNodes)
            {
                var crsText = crsNode.InnerText;
                crsText = crsText == "CRS:84" ? "CRS84" : crsText;
                var hasCRS = CoordinateSystems.FindCoordinateSystem(crsText, out var crs);
                if (hasCRS)
                {
                    supportedCrsDictionary.Add(crs, crsNode.InnerText);
                }
            }

            //try to get the preferred crs out of the list of available crses  
            foreach (var crs in preferredCRS)
            {
                if (supportedCrsDictionary.ContainsKey(crs))
                {
                    crsToUse = supportedCrsDictionary[crs];
                    return true;
                }
            }

            // our preferred crses are not available, use the first supported one
            if (supportedCrsDictionary.Count > 0)
            {
                crsToUse = supportedCrsDictionary.Values.First();
                return true;
            }

            //none of the crsses available in the WMS are supported by us
            crsToUse = null;
            return false;
        }

        private MapFilters CreateMapTemplate(int width, int height, bool transparent)
        {
            var version = GetVersion();
            return new MapFilters
            {
                version = version,
                width = width,
                height = height,
                transparent = transparent,
                spatialReferenceType = MapFilters.SpatialReferenceTypeFromVersion(new Version(version))
            };
        }

        private IEnumerable<string> ExtractStyles(XmlNode layerNode)
        {
            var styleNodes = GetMultipleNodesByName(layerNode, "Style");

            var styles = new List<string>();
            foreach (XmlNode styleNode in styleNodes)
            {
                var styleNameNode = GetInnerTextForNode(styleNode, "Name");
                if (string.IsNullOrEmpty(styleNameNode)) continue;

                styles.Add(styleNameNode);
            }

            return styles;
        }
        public Dictionary<string, string> GetLegendUrls()
        {
            Dictionary<string, string> legendUrls = new Dictionary<string, string>();
            XmlNodeList layers = xmlDocument.GetElementsByTagName("Layer");

            foreach (XmlNode layer in layers)
            {
                string layerName = layer.SelectSingleNode("*[local-name()='Name']", namespaceManager)?.InnerText;
                if (string.IsNullOrEmpty(layerName)) continue;

                XmlNodeList legendNodes = layer.SelectNodes(".//*[local-name()='LegendURL']/*[local-name()='OnlineResource']", namespaceManager);
                foreach (XmlNode legendNode in legendNodes)
                {
                    string legendUrl = legendNode.Attributes["xlink:href"]?.Value;
                    if (!string.IsNullOrEmpty(legendUrl) && !legendUrls.ContainsKey(layerName))
                    {
                        legendUrls.Add(layerName, legendUrl);
                    }
                }
            }

            return legendUrls;
        }
    }
}