using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Utility;
using System.Xml;

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
        
        public bool CapableOfBoundingBoxes => xmlDocument.SelectSingleNode("//*[local-name()='WGS84BoundingBox' or local-name()='BoundingBox']", namespaceManager) != null;

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

            var coverageNodes = xmlDocument.SelectNodes(
                "//*[local-name()='CoverageSummary']",
                namespaceManager);

            foreach (XmlNode coverageNode in coverageNodes)
            {
                var coverageId = coverageNode.SelectSingleNode(
                    "*[local-name()='CoverageId']",
                    namespaceManager);

                if (coverageId != null && !string.IsNullOrEmpty(coverageId.InnerText))
                {
                    coverageNames.Add(coverageId.InnerText);
                }
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

            var coverageNodes = xmlDocument.SelectNodes(
                "//*[local-name()='CoverageSummary']",
                namespaceManager);

            foreach (XmlNode coverageNode in coverageNodes)
            {
                var coverageId = coverageNode.SelectSingleNode(
                    "*[local-name()='CoverageId']",
                    namespaceManager)?.InnerText;

                if (string.IsNullOrEmpty(coverageId))
                    continue;

                var bboxNode = coverageNode.SelectSingleNode(
                    ".//*[local-name()='WGS84BoundingBox']",
                    namespaceManager);

                if (bboxNode == null)
                    continue;

                var lower = bboxNode.SelectSingleNode(
                    "*[local-name()='LowerCorner']",
                    namespaceManager)?.InnerText;

                var upper = bboxNode.SelectSingleNode(
                    "*[local-name()='UpperCorner']",
                    namespaceManager)?.InnerText;

                if (string.IsNullOrEmpty(lower) || string.IsNullOrEmpty(upper))
                    continue;

                var lowerValues = lower.Split(' ');
                var upperValues = upper.Split(' ');

                if (lowerValues.Length < 2 || upperValues.Length < 2)
                    continue;

                var bbox = new BoundingBox(
                    new Coordinate(
                        CoordinateSystem.CRS84,
                        double.Parse(lowerValues[0]),
                        double.Parse(lowerValues[1])),
                    new Coordinate(
                        CoordinateSystem.CRS84,
                        double.Parse(upperValues[0]),
                        double.Parse(upperValues[1]))
                );

                container.LayerBoundingBoxes[coverageId] = bbox;
            }

            boundingBoxContainer = container;
            return container;
        }
    }
}