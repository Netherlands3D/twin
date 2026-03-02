using System.Collections.Generic;
using System.Linq;
using Clipper2Lib;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public static class PolygonUtility
    {
        private static float minStrokeWidth = -1f;
        
        public static Vector2[] CoordinatesToVertices(List<Coordinate> coordinates, float lineWidth = 0)
        {
            var positions = coordinates.ToUnityPositions().ToList();
            var vertices = PolygonCalculator.FlattenPolygon(positions, new Plane(Vector3.up, 0));
            if (vertices.Length == 2)
            {
                vertices = LineToPolygon(vertices, lineWidth);
            }

            return vertices;
        }
        
        private static Vector2[] LineToPolygon(Vector2[] vertices, float width)
        {
            if (vertices.Length != 2)
            {
                Debug.LogError("cannot create rectangle because position list contains more than 2 entries");
                return null;
            }

            var dir = vertices[1] - vertices[0];
            var normal = new Vector2(-dir.y, dir.x).normalized;

            var dist = normal * width / 2;

            var point1 = vertices[0] + new Vector2(dist.x, dist.y);
            var point4 = vertices[1] + new Vector2(dist.x, dist.y);
            var point3 = vertices[1] - new Vector2(dist.x, dist.y);
            var point2 = vertices[0] - new Vector2(dist.x, dist.y);

            var polygon = new Vector2[]
            {
                point1,
                point2,
                point3,
                point4
            };

            return polygon;
        }

        public static List<CompoundPolygon> CalculatePolygons(FillType fillType, CompoundPolygon basePolygon, float strokeWidth)
        {
            var strokePolygons = new List<CompoundPolygon>();

            if (fillType == FillType.Stroke && strokeWidth > minStrokeWidth)
            {
                Debug.Log("width too narrow, not creating any holes");
                return strokePolygons;
            }

            List<Vector2[]> inflatedPathsAsArrays = InflatePaths(basePolygon, strokeWidth);

            if (fillType == FillType.Stroke)
            {
                //var strokePolygon = Polygon;
                var strokePolygon = new CompoundPolygon(basePolygon); //use new to make a deep copy of the lists in the polygon

                foreach (var hole in inflatedPathsAsArrays)
                {
                    strokePolygon.AddHole(hole);
                }

                strokePolygons.Add(strokePolygon);
                return strokePolygons;
            }

            var fillPolygons = new List<CompoundPolygon>();
            foreach (var path in inflatedPathsAsArrays)
            {
                fillPolygons.Add(new CompoundPolygon(path));
            }

            return fillPolygons;
        }

        private static List<Vector2[]> InflatePaths(CompoundPolygon polygon, float strokeWidth)
        {
            Path64 path = new Path64();
            var solidPolygon = polygon.SolidPolygon;
            for (int i = 0; i < solidPolygon.Length; i++)
            {
                Vector3 p = solidPolygon[i];
                path.Add(new Point64(p.x, p.y));
            }

            Paths64 paths = new Paths64 { path };
            var inflatedPaths = Clipper.InflatePaths(paths, strokeWidth, JoinType.Round, EndType.Polygon);

            var inflatedPathsAsArrays = new List<Vector2[]>();
            foreach (var p in inflatedPaths)
            {
                var vector2Array = ConvertToVector2Array(p);
                inflatedPathsAsArrays.Add(vector2Array);
            }

            return inflatedPathsAsArrays;
        }

        public static Vector2[] ConvertToVector2Array(Path64 path)
        {
            var array = new Vector2[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                Point64 p = path[i];
                var vec = new Vector2(p.X, p.Y);
                array[i] = vec;
            }

            return array;
        }
    }
}