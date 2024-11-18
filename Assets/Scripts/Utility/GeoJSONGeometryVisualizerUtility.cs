using System.Collections.Generic;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
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

        /// <summary>
        /// the polygon vertex positions are offseted back by its polygon centroid and added to the transform of the returned polygonvisualisation transform position
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="originalCoordinateSystem"></param>
        /// <param name="visualizationMaterial"></param>
        /// <returns></returns>
        public static PolygonVisualisation VisualizePolygon(Polygon polygon, CoordinateSystem originalCoordinateSystem, Material visualizationMaterial)
        {            
            var ringList = new List<List<Vector3>>(polygon.Coordinates.Count);
            Vector3 centroid = Vector3.zero;
            foreach (var lineString in polygon.Coordinates)
            {
                var ring = ConvertToCoordinates(lineString, originalCoordinateSystem);
                int ringCount = ring.Count;
                foreach (var coord in ring)
                {
                    centroid += coord.ToUnity() / ringCount;
                }
            }
            centroid /= polygon.Coordinates.Count;

            foreach (var lineString in polygon.Coordinates)
            {
                var ring = ConvertToCoordinates(lineString, originalCoordinateSystem);
                var unityRing = new List<Vector3>(ring.Count);
                foreach (var coord in ring)
                {
                    Vector3 c = coord.ToUnity() - centroid;
                    unityRing.Add(c);
                }
                ringList.Add(unityRing);
            }
            PolygonVisualisation polygonVisualisation = CreatePolygonMesh(ringList, 10f, visualizationMaterial);
            polygonVisualisation.transform.position += centroid;
            return polygonVisualisation;
        }

        public static List<List<Coordinate>> VisualizeMultiLineString(MultiLineString multiLineString, CoordinateSystem originalCoordinateSystem, LineRenderer3D renderer)
        {
            var lines = new List<List<Coordinate>>(multiLineString.Coordinates.Count);
            foreach (var lineString in multiLineString.Coordinates)
            {
                var convertedLineString = ConvertToCoordinates(lineString, originalCoordinateSystem);
                lines.Add(convertedLineString);
            }
            renderer.AppendLines(lines);

            return lines;
        }

        public static List<Coordinate> VisualizeLineString(LineString lineString, CoordinateSystem originalCoordinateSystem, LineRenderer3D renderer)
        {
            var line = ConvertToCoordinates(lineString, originalCoordinateSystem);
            renderer.AppendLine(line);

            return line;
        }

        public static List<Coordinate> VisualizeMultiPoint(MultiPoint multipoint, CoordinateSystem coordinateSystem, BatchedMeshInstanceRenderer renderer)
        {
            var convertedPoints = ConvertToCoordinates(multipoint, coordinateSystem);
            renderer.AppendCollection(convertedPoints);

            return convertedPoints;
        }

        public static List<Coordinate> VisualizePoint(Point point, CoordinateSystem coordinateSystem, BatchedMeshInstanceRenderer renderer)
        {
            var convertedPoint = ConvertToCoordinate(coordinateSystem, point.Coordinates);
            var singlePointList = new List<Coordinate>() { convertedPoint };
            renderer.AppendCollection(singlePointList);

            return singlePointList;
        }

        public static PolygonVisualisation CreatePolygonMesh(List<List<Vector3>> contours, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, false, false, false, polygonMeshMaterial);

            //Add the polygon shifter to the polygon visualisation, so it can move with our origin shifts
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("Projected");
            polygonVisualisation.gameObject.AddComponent<WorldTransform>(); 

            return polygonVisualisation;
        }

        private static List<Coordinate> ConvertToCoordinates(LineString lineString, CoordinateSystem originalCoordinateSystem, float defaultNAPHeight = 0)
        {
            var convertedCoordinates = new List<Coordinate>(lineString.Coordinates.Count);

            for (var i = 0; i < lineString.Coordinates.Count; i++)
            {
                var point = lineString.Coordinates[i];
                var coordinate = ConvertToCoordinate(originalCoordinateSystem, point, defaultNAPHeight);
                convertedCoordinates.Add(coordinate);
            }

            return convertedCoordinates;
        }
        
        private static List<Coordinate> ConvertToCoordinates(MultiPoint multiPoint, CoordinateSystem originalCoordinateSystem, float defaultNAPHeight = 0)
        {
            var convertedCoordinates = new List<Coordinate>(multiPoint.Coordinates.Count);

            for (var i = 0; i < multiPoint.Coordinates.Count; i++)
            {
                var point = multiPoint.Coordinates[i];
                var coordinate = ConvertToCoordinate(originalCoordinateSystem, point.Coordinates, defaultNAPHeight);
                convertedCoordinates.Add(coordinate);
            }
            return convertedCoordinates;
        }

        private static Coordinate ConvertToCoordinate(CoordinateSystem originalCoordinateSystem, IPosition point, float defaultNAPHeight = 0)
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

            return coord;
        }
    }
}