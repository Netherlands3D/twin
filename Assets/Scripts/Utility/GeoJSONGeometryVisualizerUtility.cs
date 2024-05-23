using System.Collections.Generic;
using System.Windows.Forms;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public static class GeoJSONGeometryVisualizerUtility
    {
        public static List<PolygonVisualisation> VisualizeMultiPolygon(MultiPolygon multiPolygon, CoordinateSystem originalCoordinateSystem, Material visualizationMaterial)
        {
            var visualizations = new List<PolygonVisualisation>(multiPolygon.Coordinates.Count);
            foreach (var polygon in multiPolygon.Coordinates)
            {
                var visualization = VisualizePolygon(polygon, originalCoordinateSystem, visualizationMaterial);
                visualizations.Add(visualization);
            }

            return visualizations;
        }

        public static PolygonVisualisation VisualizePolygon(Polygon polygon, CoordinateSystem originalCoordinateSystem, Material visualizationMaterial)
        {
            var ringList = new List<List<Vector3>>();

            foreach (var lineString in polygon.Coordinates)
            {
                var ring = ConvertToUnityCoordinates(lineString, originalCoordinateSystem);
                ringList.Add(ring);
            }

            return CreatePolygonMesh(ringList, 10f, visualizationMaterial);
        }

        public static void VisualizeMultiLineString(MultiLineString multiLineString)
        {
            foreach (var lineString in multiLineString.Coordinates)
            {
                VisualizeLineString(lineString);
            }
        }

        public static void VisualizeLineString(LineString lineString)
        {
        }

        public static PolygonVisualisation CreatePolygonMesh(List<List<Vector3>> contours, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, false, false, false, polygonMeshMaterial);

            //Add the polygon shifter to the polygon visualisation, so it can move with our origin shifts
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("ScatterPolygons");

            return polygonVisualisation;
        }

        private static List<Vector3> ConvertToUnityCoordinates(LineString lineString, CoordinateSystem originalCoordinateSystem, float defaultHeight = 0)
        {
            var convertedCoordinates = new List<Vector3>(lineString.Coordinates.Count);
            // Vector3 unityCoord2D = new Vector3();

            for (var i = 0; i < lineString.Coordinates.Count; i++)
            {
                var point = lineString.Coordinates[i];
                var lat = point.Latitude;
                var lon = point.Longitude;
                var alt = point.Altitude;

                Coordinate coord;
                if (alt == null)
                    alt = defaultHeight;

                coord = new Coordinate(originalCoordinateSystem, lat, lon, (double)alt);

                var unityCoord = CoordinateConverter.ConvertTo(coord, CoordinateSystem.Unity).ToVector3();
                // unityCoord2D.x = unityCoord.x;
                // unityCoord2D.y = unityCoord.z;
                convertedCoordinates.Add(unityCoord);
            }

            return convertedCoordinates;
        }
    }
}
