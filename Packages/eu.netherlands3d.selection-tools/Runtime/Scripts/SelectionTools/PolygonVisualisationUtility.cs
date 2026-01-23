using System;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.Triangulate.Polygon;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.SelectionTools
{
    public class GeometryTriangulationData
    {
        public GeometryTriangulationData(Geometry geometry, Vector3 origin, Vector3 u, Vector3 v /*, Vector3 normal*/)
        {
            this.geometry = geometry;
            this.origin = origin;
            this.u = u;
            this.v = v;
            // this.normal = normal;
        }

        public Geometry geometry;
        public Vector3 origin;
        public Vector3 u;
        public Vector3 v;

        // public Vector3 normal;
    }

    public static class PolygonVisualisationUtility
    {
        private static NtsGeometryServices instance;
        private static GeometryFactory geometryFactory;

        static PolygonVisualisationUtility()
        {
            //the following are the default proposed settings by NTS github page https://github.com/NetTopologySuite/NetTopologySuite/wiki/GettingStarted
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

            newPolygonObject.AddComponent<MeshFilter>(); //mesh is created by the PolygonVisualisation script
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
        public static GeometryTriangulationData CreatePolygonGeometryTriangulationData(List<List<Vector3>> contours, bool invertWindingOrder = false)
        {
            if (contours == null || contours.Count == 0 || contours[0].Count < 3)
                return null;
            
            // STEP 1: Compute polygon plane
            var coordsArray = contours[0].ToList(); //We have to reverse the contour to make the computebestfitplane work because of the algorithm
            if (!invertWindingOrder)
            {
                coordsArray.Reverse();
            }
            
            var solidPlane = coordsArray;
            Plane plane = ComputeBestFitPlane(solidPlane);
            Vector3 normal = plane.normal.normalized;
            Vector3 tangent = (Mathf.Abs(normal.x) > 0.1f || Mathf.Abs(normal.z) > 0.1f) ? Vector3.up : Vector3.right;
            Vector3 u = Vector3.Cross(tangent, normal).normalized;
            Vector3 v = Vector3.Cross(normal, u);
            Vector3 origin = contours[0][0];
            
            // === STEP 2: Build NTS polygon ===
            LinearRing outerRing = geometryFactory.CreateLinearRing(ConvertToCoordinateArray(contours[0], origin, u, v, !invertWindingOrder));
            LinearRing[] holes = new LinearRing[contours.Count - 1];
            for (int h = 1; h < contours.Count; h++)
                holes[h - 1] = geometryFactory.CreateLinearRing(ConvertToCoordinateArray(contours[h], origin, u, v, invertWindingOrder));

            var polygon = geometryFactory.CreatePolygon(outerRing, holes);
            
            // todo: this check is needed, but very garbage intensive if possible use a different check to determine polygon validity
            // a try catch block is not possible, since it does not work in webgl and the app can end up in an infinite loop
            if (!polygon.IsValid) 
            {
                return null;
            }
            // STEP 3: Triangulate in 2D
            Geometry triangulated = ConstrainedDelaunayTriangulator.Triangulate(polygon);
            return new GeometryTriangulationData(triangulated, origin, u, v /*, normal*/);
        }

        public static Mesh CreatePolygonMesh(
            List<GeometryTriangulationData> datas,
            Vector3 offset
        )
        {
            List<Vector3> verts = new();
            List<int> tris = new();

            for (int i = 0; i < datas.Count; i++)
            {
                var data = datas[i];
                if (data == null) continue;

                for (int j = 0; j < data.geometry.NumGeometries; j++)
                {
                    var geom = data.geometry.GetGeometryN(j);
                    if (geom is not Polygon poly) continue;

                    var coords = poly.Coordinates;

                    Vector3 v0 = To3D(coords[0], data.origin, data.u, data.v) - offset;
                    Vector3 v1 = To3D(coords[1], data.origin, data.u, data.v) - offset;
                    Vector3 v2 = To3D(coords[2], data.origin, data.u, data.v) - offset;

                    int baseIndex = verts.Count;

                    verts.Add(v0);
                    verts.Add(v1);
                    verts.Add(v2);

                    tris.Add(baseIndex);
                    tris.Add(baseIndex + 1);
                    tris.Add(baseIndex + 2);
                }
            }

            Mesh mesh = new Mesh
            {
                vertices = verts.ToArray(),
                triangles = tris.ToArray()
            };
            
           

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreatePolygonMesh(List<List<Vector3>> contours, bool invertWindingOrder = false)
        {
            var triangulationData = CreatePolygonGeometryTriangulationData(contours, invertWindingOrder); 
            return CreatePolygonMesh(new List<GeometryTriangulationData>() { triangulationData }, Vector3.zero);
        }

        private static Coordinate[] ConvertToCoordinateArray(List<Vector3> points, Vector3 origin, Vector3 u, Vector3 v, bool shouldBeCCW)
        {
            var coords = new List<Coordinate>(points.Count + 1);
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 d = points[i] - origin;
                coords.Add(new Coordinate(Vector3.Dot(d, u), Vector3.Dot(d, v)));
            }

            if (!coords[0].Equals2D(coords[^1]))
            {
                coords.Add(coords[0]);
            }

            var coordsArray = coords.ToArray();
            var isCCW = NetTopologySuite.Algorithm.Orientation.IsCCW(coordsArray);
            if (isCCW ^ shouldBeCCW)
            {
                Array.Reverse(coordsArray);
            }

            return coordsArray;
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

        /// <summary>
        /// Reconstructs a 3D point from its 2D plane coordinate.
        /// </summary>
        private static Vector3 To3D(Coordinate c, Vector3 origin, Vector3 u, Vector3 v)
        {
            return origin + (float)c.X * u + (float)c.Y * v;
        }

        /// <summary>
        /// Computes a best-fit plane for a polygon (assumes it's planar).
        private static Plane ComputeBestFitPlane(List<Vector3> contour)
        {
            if (contour.Count < 3)
                throw new ArgumentException("Need at least 3 points for a plane");
            
            // Newell's method
            Vector3 normal = Vector3.zero;
            int n = contour.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 current = contour[i];
                Vector3 next = contour[(i + 1) % n];
                normal.x += (current.y - next.y) * (current.z + next.z);
                normal.y += (current.z - next.z) * (current.x + next.x);
                normal.z += (current.x - next.x) * (current.y + next.y);
            }
            normal.Normalize();
            return new Plane(normal, contour[0]);
        }
    }
}