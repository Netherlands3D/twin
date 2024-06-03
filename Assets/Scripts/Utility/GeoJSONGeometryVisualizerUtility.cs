using System.Collections.Generic;
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
            var ringList = new List<List<Vector3>>(polygon.Coordinates.Count);

            foreach (var lineString in polygon.Coordinates)
            {
                var ring = ConvertToUnityCoordinates(lineString, originalCoordinateSystem);
                ringList.Add(ring);
            }

            return CreatePolygonMesh(ringList, 10f, visualizationMaterial);
        }

        public static void VisualizeMultiLineString(MultiLineString multiLineString, CoordinateSystem originalCoordinateSystem, LineRenderer3D renderer)
        {
            var convertedLineStrings = new List<List<Vector3>>(multiLineString.Coordinates.Count);
            foreach (var lineString in multiLineString.Coordinates)
            {
                var convertedLineString = ConvertToUnityCoordinates(lineString, originalCoordinateSystem);
                convertedLineStrings.Add(convertedLineString);
            }

            renderer.AppendLines(convertedLineStrings);
        }

        public static void VisualizeLineString(LineString lineString, CoordinateSystem originalCoordinateSystem, LineRenderer3D renderer)
        {
            var convertedLineString = ConvertToUnityCoordinates(lineString, originalCoordinateSystem);
            renderer.AppendLine(convertedLineString);
        }

        public static void VisualizeMultiPoint(MultiPoint multipoint, CoordinateSystem coordinateSystem, BatchedMeshInstanceRenderer renderer)
        {
            var convertedPoints = ConvertToUnityCoordinates(multipoint, coordinateSystem);
            renderer.AppendCollection(convertedPoints);
        }

        public static void VisualizePoint(Point point, CoordinateSystem coordinateSystem, BatchedMeshInstanceRenderer renderer)
        {
            var convertedPoint = ConvertToUnityCoordinate(coordinateSystem, point.Coordinates);
            renderer.AppendCollection(new List<Vector3>() { convertedPoint });
        }

        public static PolygonVisualisation CreatePolygonMesh(List<List<Vector3>> contours, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, false, false, false, polygonMeshMaterial);

            //Add the polygon shifter to the polygon visualisation, so it can move with our origin shifts
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("Projected");

            return polygonVisualisation;
        }

        private static List<Vector3> ConvertToUnityCoordinates(LineString lineString, CoordinateSystem originalCoordinateSystem, float defaultNAPHeight = 0)
        {
            var convertedCoordinates = new List<Vector3>(lineString.Coordinates.Count);

            for (var i = 0; i < lineString.Coordinates.Count; i++)
            {
                var point = lineString.Coordinates[i];
                var unityCoord = ConvertToUnityCoordinate(originalCoordinateSystem, point, defaultNAPHeight);
                convertedCoordinates.Add(unityCoord);
            }

            return convertedCoordinates;
        }
        
        private static List<Vector3> ConvertToUnityCoordinates(MultiPoint multiPoint, CoordinateSystem originalCoordinateSystem, float defaultNAPHeight = 0)
        {
            var convertedCoordinates = new List<Vector3>(multiPoint.Coordinates.Count);

            for (var i = 0; i < multiPoint.Coordinates.Count; i++)
            {
                var point = multiPoint.Coordinates[i];
                var unityCoord = ConvertToUnityCoordinate(originalCoordinateSystem, point.Coordinates, defaultNAPHeight);
                convertedCoordinates.Add(unityCoord);
            }
            return convertedCoordinates;
        }

        private static Vector3 ConvertToUnityCoordinate(CoordinateSystem originalCoordinateSystem, IPosition point, float defaultNAPHeight = 0)
        {
            var lat = point.Latitude;
            var lon = point.Longitude;
            var alt = point.Altitude;

            Coordinate coord = new Coordinate(originalCoordinateSystem);
            coord.easting = lon;
            coord.northing = lat;
            if (alt != null)
            {
                coord.height = (double)alt;
            }
            else
            {
                coord = coord.Convert(CoordinateSystem.RDNAP);
                coord.height = defaultNAPHeight;
            }

            return coord.ToUnity();
        }
    }
}