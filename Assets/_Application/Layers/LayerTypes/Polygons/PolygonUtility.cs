using System.Collections.Generic;
using Clipper2Lib;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class PolygonUtility : MonoBehaviour
    {
        private static float minStrokeWidth = -1f;

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