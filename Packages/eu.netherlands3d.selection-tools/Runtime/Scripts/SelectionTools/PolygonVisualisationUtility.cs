using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.Triangulate.Polygon;
using NetTopologySuite.Triangulate.Tri;
using System.Collections.Generic;
using UnityEngine;


namespace Netherlands3D.SelectionTools
{
    public static class PolygonVisualisationUtility
    {
        private static NtsGeometryServices instance;
        private static GeometryFactory geometryFactory;       

        private static void SetUpInstance()
        {
            if (instance != null)
                return;

            instance = new NtsGeometryServices(
            // default CoordinateSequenceFactory
            CoordinateArraySequenceFactory.Instance,
            // default precision model
            new PrecisionModel(1000d),
            // default SRID
            4326,
            /********************************************************************
             * Note: the following arguments are only valid for NTS >= v2.2
             ********************************************************************/
            // Geometry overlay operation function set to use (Legacy or NG)
            GeometryOverlay.NG,
            // Coordinate equality comparer to use (CoordinateEqualityComparer or PerOrdinateEqualityComparer)
            new CoordinateEqualityComparer());

            geometryFactory = instance.CreateGeometryFactory();
        }

        #region UnityComponents
        //Treat first contour as outer contour, and extra contours as holes
        public static PolygonVisualisation CreateAndReturnPolygonObject(List<List<Vector3>> contours,
            float meshExtrusionHeight,
            bool addMeshColliders,
            bool createInwardMesh = false,
            bool addBottomToMesh = true,
            Material meshMaterial = null,
            Material lineMaterial = null,
            Color lineColor = default,
            Vector2 uvCoordinate = new Vector2(),
            bool receiveShadows = true
        )
        {
            var newPolygonObject = new GameObject();
            newPolygonObject.name = "PolygonVisualisation";
            
            var meshFilter = newPolygonObject.AddComponent<MeshFilter>(); //mesh is created by the PolygonVisualisation script
            var meshRenderer = newPolygonObject.AddComponent<MeshRenderer>();
            meshRenderer.material = meshMaterial;
            meshRenderer.receiveShadows = receiveShadows;

            if (addMeshColliders)
                newPolygonObject.AddComponent<MeshCollider>();

            var polygonVisualisation = newPolygonObject.AddComponent<PolygonVisualisation>();
            polygonVisualisation.Initialize(contours, meshExtrusionHeight, addBottomToMesh, createInwardMesh, lineMaterial, lineColor, uvCoordinate);
            newPolygonObject.transform.Translate(0, meshExtrusionHeight, 0);

            return polygonVisualisation;
        }

        #endregion

        #region PolygonMesh
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contours"></param>
        /// <param name="extrusionHeight"></param>
        /// <param name="addBottom"></param>
        /// <param name="uvCoordinate"></param>
        /// <returns></returns>
        public static Mesh CreatePolygonMesh(List<List<Vector3>> contours, float extrusionHeight, bool addBottom, Vector2 uvCoordinate = new Vector2())
        {
            if (contours.Count == 0)
                return null;

            SetUpInstance();

            var shellCoords = EnsureOrientation(contours[0], true);
            if (shellCoords == null)
            {
                Debug.LogWarning("Outer shell is degenerate");
                return null;
            }
            var shell = geometryFactory.CreateLinearRing(shellCoords);
            LinearRing[] holes = null;
            //holes
            if (contours.Count > 1)
            {
                holes = new LinearRing[contours.Count - 1];
                for (int i = 1; i < contours.Count; i++)
                {
                    var holeCoords = EnsureOrientation(contours[i], false);
                    if (holeCoords == null)
                    {
                        Debug.LogWarning("Skipping degenerate hole at index " + i);
                        continue;
                    }
                    holes[i - 1] = geometryFactory.CreateLinearRing(holeCoords);
                }
            }
            Polygon polygon = geometryFactory.CreatePolygon(shell, holes);
            Mesh mesh = PolygonToMesh(polygon);
            return mesh;
        }

        private static Coordinate[] EnsureOrientation(List<Vector3> points, bool counterClockwise)
        {
            var coords = ConvertToCoordinateArray(points);
            if (NetTopologySuite.Algorithm.Orientation.IsCCW(coords) != counterClockwise)
            {
                System.Array.Reverse(coords);
            }
            return coords;
        }

        private static Coordinate[] ConvertToCoordinateArray(List<Vector3> points)
        {
            //check duplicates
            var filtered = new List<Coordinate>();
            Coordinate? last = null;
            foreach (var pt in points)
            {
                var current = new Coordinate(pt.x, pt.z);
                if (last == null || !current.Equals2D(last))
                {
                    filtered.Add(current);
                    last = current;
                }
            }
            if (filtered.Count < 3)
                return null;

            var coords = new Coordinate[filtered.Count + 1];
            for (int i = 0; i < filtered.Count; i++)
            {
                coords[i] = filtered[i];
            }
            coords[filtered.Count] = coords[0];
            return coords;
        }

        public static Mesh PolygonToMesh(Polygon polygon)
        {
            //triangulate
            var cleaner = new NetTopologySuite.Simplify.TopologyPreservingSimplifier(polygon);
            cleaner.DistanceTolerance = 0.01;
            var cleanPolygon = cleaner.GetResultGeometry() as Polygon;
            if (!cleanPolygon.IsValid)
            {
                Debug.LogError("Polygon is invalid: " + cleanPolygon.ToString());
                return null;
            }
            var triangulator = new PolygonTriangulator(cleanPolygon);
            List<Tri> triangles = triangulator.GetTriangles(); 
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();
            Dictionary<Coordinate, int> coordToIndex = new Dictionary<Coordinate, int>(new CoordinateEqualityComparer());
            int nextIndex = 0;
            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                Coordinate[] coords = new[] { tri.GetCoordinate(0), tri.GetCoordinate(1), tri.GetCoordinate(2) };
                for (int j = 0; j < 3; j++)
                {
                    var coord = coords[j];
                    if (!coordToIndex.TryGetValue(coord, out int index))
                    {
                        index = nextIndex++;
                        coordToIndex[coord] = index;
                        vertices.Add(new Vector3((float)coord.X, 0f, (float)coord.Y));
                    }
                    indices.Add(index);
                }
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        #endregion

        #region PolygonLine
        public static List<LineRenderer> CreateLineRenderers(List<List<Vector3>> polygons, Material lineMaterial, Color lineColor, Transform parent = null)
        {
            var list = new List<LineRenderer>();
            foreach (var contour in polygons)
            {
                list.Add(CreateAndReturnPolygonLine((List<Vector3>)contour, lineMaterial, lineColor, parent)); //todo: require explicit List<List<Vector3>> as argument?
            }
            return list;
        }

        public static LineRenderer CreateAndReturnPolygonLine(List<Vector3> contour, Material lineMaterial, Color lineColor, Transform parent = null)
        {
            var newPolygonObject = new GameObject();
            newPolygonObject.transform.SetParent(parent);
            newPolygonObject.name = "PolygonOutline";
            var lineRenderer = newPolygonObject.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;

            lineRenderer.loop = true;

            lineRenderer.positionCount = contour.Count;
            lineRenderer.SetPositions(contour.ToArray()); //does not work for some reason

            return lineRenderer;
        }
        #endregion

        public static void SetUVCoordinates(Mesh newPolygonMesh, Vector2 uvCoordinate)
        {
            var uvs = new Vector2[newPolygonMesh.vertexCount];
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = uvCoordinate;
            }
            newPolygonMesh.uv = uvs;
        }
    }
}
