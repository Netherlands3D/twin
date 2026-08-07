using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Utility;
using System.Xml;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wcs
{
    public class WcsGetCapabilities : BaseRequest, IGetCapabilities
    {
        public Uri GetCapabilitiesUri => Url;

        public const string DefaultFallbackVersion = "2.0.1";

        public ServiceType ServiceType => ServiceType.Wcs;

        protected override Dictionary<string, string> defaultNameSpaces => new()
        {
            { "ows", "http://www.opengis.net/ows/1.1" },
            { "wcs", "http://www.opengis.net/wcs/2.0" }
        };
        
        public bool CapableOfBoundingBoxes =>
            xmlDocument.SelectSingleNode(
                "//*[local-name()='lonLatEnvelope']",
                namespaceManager
            ) != null;

        public WcsGetCapabilities(Uri url, string xml) : base(url, xml)
        {
        }

        public string GetVersion()
        {
            var versionInXml = xmlDocument.DocumentElement.GetAttribute("version");

            return !string.IsNullOrEmpty(versionInXml)
                ? versionInXml
                : DefaultFallbackVersion;
        }

        public string GetTitle()
        {
            return GetInnerTextForNode(xmlDocument.DocumentElement, "Title");
        }

        public List<string> GetLayerNames()
        {
            var coverageNames = new List<string>();

            var nodes = xmlDocument.SelectNodes(
                "//*[local-name()='CoverageOfferingBrief']/*[local-name()='name']",
                namespaceManager
            );

            foreach (XmlNode node in nodes)
            {
                if (!string.IsNullOrEmpty(node.InnerText))
                    coverageNames.Add(node.InnerText);
            }

            return coverageNames;
        }

        public bool HasBounds
        {
            get
            {
                var bounds = GetBounds();

                return bounds.GlobalBoundingBox != null ||
                       bounds.LayerBoundingBoxes.Count > 0;
            }
        }

        private BoundingBoxContainer boundingBoxContainer;

        public BoundingBoxContainer GetBounds()
        {
            if (boundingBoxContainer != null)
                return boundingBoxContainer;

            var container = new BoundingBoxContainer(Url.ToString());

            // WCS 1.0 uses CoverageOfferingBrief
            var coverageNodes = xmlDocument.SelectNodes(
                "//*[local-name()='CoverageOfferingBrief']");

            if (coverageNodes == null)
            {
                boundingBoxContainer = container;
                return container;
            }

            foreach (XmlNode coverageNode in coverageNodes)
            {
                // WCS 1.0 uses <name>
                var coverageId = coverageNode.SelectSingleNode(
                    "*[local-name()='name']")?.InnerText;

                if (string.IsNullOrEmpty(coverageId))
                    continue;

                // WCS 1.0 uses lonLatEnvelope
                var bboxNode = coverageNode.SelectSingleNode(
                    ".//*[local-name()='lonLatEnvelope']");

                if (bboxNode == null)
                    continue;

                var bbox = ParseBoundingBox(
                    bboxNode,
                    CoordinateSystem.CRS84);

                if (bbox == null)
                    continue;

                container.LayerBoundingBoxes[coverageId] = bbox;

                // Some consumers expect a global bbox fallback
                if (container.GlobalBoundingBox == null)
                    container.GlobalBoundingBox = bbox;
            }

            boundingBoxContainer = container;
            return container;
        }

        private BoundingBox ParseBoundingBox(XmlNode node, CoordinateSystem crs)
        {
            if (node == null)
                return null;

            var positions = node.SelectNodes(
                "*[local-name()='pos']");

            if (positions == null || positions.Count < 2)
                return null;

            var lower = positions[0].InnerText
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            var upper = positions[1].InnerText
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            if (lower.Length < 2 || upper.Length < 2)
                return null;

            if (!double.TryParse(
                    lower[0],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var minX))
                return null;

            if (!double.TryParse(
                    lower[1],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var minY))
                return null;

            if (!double.TryParse(
                    upper[0],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var maxX))
                return null;

            if (!double.TryParse(
                    upper[1],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var maxY))
                return null;

            return new BoundingBox(
                new Coordinate(
                    crs,
                    minX,
                    minY),
                new Coordinate(
                    crs,
                    maxX,
                    maxY));
        }
        
        public string GetSpatialReference(string coverageName)
        {
            var coverageNode = xmlDocument.SelectSingleNode(
                $"//*[local-name()='CoverageOfferingBrief']" +
                $"[*[local-name()='name' and text()='{coverageName}']]",
                namespaceManager
            );

            if (coverageNode == null)
            {
                Debug.LogWarning($"WCS coverage '{coverageName}' was not found in capabilities.");
                return null;
            }

            var envelopeNode = coverageNode.SelectSingleNode(
                "*[local-name()='lonLatEnvelope']",
                namespaceManager
            );

            var srsName = envelopeNode?.Attributes?["srsName"]?.Value;

            if (string.IsNullOrEmpty(srsName))
            {
                Debug.LogWarning($"WCS coverage '{coverageName}' has no CRS.");
                return null;
            }

            // ADAGUC advertises CRS84 but accepts EPSG:4326 for GetCoverage.
            if (srsName.Contains("CRS84", StringComparison.OrdinalIgnoreCase))
                return "EPSG:4326";

            return srsName;
        }
    }
}