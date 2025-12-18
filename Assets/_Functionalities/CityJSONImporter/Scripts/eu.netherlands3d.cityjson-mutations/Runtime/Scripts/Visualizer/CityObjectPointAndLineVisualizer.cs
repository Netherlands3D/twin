using System;
using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Rendering;
using UnityEngine;

namespace Netherlands3D.CityJson.Visualisation
{
    [RequireComponent(typeof(CityObject))]
    [RequireComponent(typeof(BatchedMeshInstanceRenderer))]
    public class CityObjectPointAndLineVisualizer : CityObjectVisualizer
    {
        [SerializeField] private GeometryType geometryType;
        [SerializeField] private BatchedMeshInstanceRenderer batchedMeshInstanceRenderer;

        public override Material[] Materials => batchedMeshInstanceRenderer.Materials;

        protected override void Visualize()
        {
            batchedMeshInstanceRenderer.Clear();
            foreach (var geometry in cityObject.Geometries)
            {
                if (geometry.Type != geometryType)
                    continue; // other types have their own visualizer and don't create meshes

                var collections = GetPositionCollections(geometry.BoundaryObject, cityObject.CoordinateSystem);
                batchedMeshInstanceRenderer.AppendCollections(collections);
            }

            cityObjectVisualized?.Invoke(this);
        }

        private static List<List<Coordinate>> GetPositionCollections(CityBoundary boundary, CoordinateSystem coordinateSystem)
        {
            List<List<Coordinate>> result = new List<List<Coordinate>>();
            switch (boundary)
            {
                case CityMultiPoint multiPoint:
                    var coordinates = ToCoordinates(multiPoint.Points, coordinateSystem);
                    result.Add(coordinates);
                    break;
                case CityMultiLineString multiLineString:
                    foreach (var line in multiLineString.LineStrings)
                    {
                        coordinates = ToCoordinates(line, coordinateSystem);
                        result.Add(coordinates);
                    }
                    break;
                default:
                    throw new NotSupportedException("Boundary of type " + boundary.GetType() + " is not supported by this visualizer");
            }

            return result;
        }

        private static List<Coordinate> ToCoordinates(CityPolygon polygon, CoordinateSystem coordinateSystem)
        {
            var coordinates = new List<Coordinate>(polygon.Count);
            foreach (var vert in polygon.Vertices)
            {
                var coord = new Coordinate(coordinateSystem, vert.x, vert.y, vert.z);
                coordinates.Add(coord);
            }

            return coordinates;
        }
        
        public override void SetFillColor(Color color)
        {
            batchedMeshInstanceRenderer.SetAllColors(color);
        }

        public override void SetLineColor(Color color)
        {
            if (batchedMeshInstanceRenderer is LineRenderer3D lineRenderer)
            {
                lineRenderer.SetAllColors(color);
            }
        }
    }
}