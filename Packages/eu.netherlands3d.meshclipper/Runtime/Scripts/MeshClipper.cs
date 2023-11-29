using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.MeshClipping
{
    public class MeshClipper
    {
        private GameObject sourceGameObject;
        private Mesh sourceMesh;
        private Vector3 sourceOrigin;
        private Vector3 boundsOrigin; //Bottom left corner of the bounding box
        private Vector3[] vertexWorldPositions;
        public List<Vector3> clippedVertices;

        /// <summary>
        /// Sets the target GameObject
        /// </summary>
        /// <param name="sourceGameObject"></param>
        public void SetGameObject(GameObject sourceGameObject)
        {
            this.sourceGameObject = sourceGameObject;
            if (!sourceGameObject.TryGetComponent<MeshFilter>(out var meshFilter))
            {
                Debug.LogError("MeshFilter not found on GameObject. A meshfilter is required to clip the mesh.");
            }

            sourceMesh = meshFilter.sharedMesh;
            sourceOrigin = sourceGameObject.transform.position;
            ReadVertices();
        }

        /// <summary>
        /// Return the clipped geometry as a list of triangles
        /// </summary>
        /// <returns>A Vector3 list where every 3 items is a single triangle</returns>
        public List<Vector3> GetTriangles()
        {
            return clippedVertices;
        }

        /// <summary>
        /// Return the clipped geometry as a mesh
        /// </summary>
        /// <returns>A new mesh with the clipped geometry</returns>
        public Mesh GetTriangleMesh(bool recalculateNormals = false, bool recalculateBounds = false)
        {
            var mesh = new Mesh
            {
                name = sourceMesh.name + "_clipped"
            };
            mesh.SetVertices(clippedVertices);
            mesh.SetTriangles(Enumerable.Range(0, clippedVertices.Count).ToArray(), 0);

            if(recalculateNormals) mesh.RecalculateNormals();
            if(recalculateBounds) mesh.RecalculateBounds();

            return mesh;
        }

        public void ClipSubMesh(Bounds boundingBox, int subMeshNumber)
        {
            clippedVertices = new List<Vector3>();
            if (subMeshNumber >= sourceMesh.subMeshCount)
            {
                return;
            }

            int[] indices = sourceMesh.GetIndices(subMeshNumber);
            boundsOrigin = new Vector3(boundingBox.center.x - boundingBox.size.x/2.0f, boundingBox.center.y - boundingBox.size.y/2.0f, boundingBox.center.z - boundingBox.size.z/2.0f);

            Vector3 point1;
            Vector3 point2;
            Vector3 point3;
            List<Vector3> clippingPolygon = CreateClippingPolygon(boundingBox);
            List<Vector3> clippingVectorList = new(){ Capacity = 3 };

            for (int i = 0; i < indices.Length; i += 3)
            {
                point1 = vertexWorldPositions[indices[i]];
                point2 = vertexWorldPositions[indices[i + 1]];
                point3 = vertexWorldPositions[indices[i + 2]];
                TrianglePosition position = GetTrianglePosition(point1, point2, point3, boundingBox);
                if (position == TrianglePosition.inside)
                {
                    clippedVertices.Add(PointWithBoundsOrigin(point1));
                    clippedVertices.Add(PointWithBoundsOrigin(point2));
                    clippedVertices.Add(PointWithBoundsOrigin(point3));
                }
                else if (position == TrianglePosition.overlap)
                {
                    clippingVectorList.Clear();
                    clippingVectorList.Add(point1);
                    clippingVectorList.Add(point2);
                    clippingVectorList.Add(point3);

                    List<Vector3> clippedTriangle = TriangleClipping.SutherlandHodgman.ClipPolygon(clippingVectorList, clippingPolygon);
                    int vertexcount = clippedTriangle.Count;
                    if (vertexcount < 3)
                    {
                        continue;
                    }
                    clippedVertices.Add(PointWithBoundsOrigin(clippedTriangle[0]));
                    clippedVertices.Add(PointWithBoundsOrigin(clippedTriangle[1]));
                    clippedVertices.Add(PointWithBoundsOrigin(clippedTriangle[2]));
                    // add extra vectors. vector makes a triangle with the first and the previous vector.
                    for (int j = 3; j < vertexcount; j++)
                    {
                        clippedVertices.Add(PointWithBoundsOrigin(clippedTriangle[0]));
                        clippedVertices.Add(PointWithBoundsOrigin(clippedTriangle[j - 1]));
                        clippedVertices.Add(PointWithBoundsOrigin(clippedTriangle[j]));
                    }
                }
            }
        }

        /// <summary>
        /// Returns a point with the bounds origin (2D center) as origin
        /// </summary>
        /// <param name="point">Original world space point</param>
        /// <returns>Point converted to coordinate within bounds</returns>
        private Vector3 PointWithBoundsOrigin(Vector3 point)
        {
            return new Vector3(point.x - boundsOrigin.x, point.y, point.z - boundsOrigin.z);
        }

        public static List<Vector3> CreateClippingPolygon(Bounds boundingBox)
        {
            List<Vector3> output = new(4)
            {
                boundingBox.min,
                new(boundingBox.max.x, 0, boundingBox.min.z),
                boundingBox.max,
                new(boundingBox.min.x, 0, boundingBox.max.z)
            };
            return output;
        }

        /// <summary>
        /// Returns if triangle is inside, outside or overlapping with boundingbox
        /// </summary>
        /// <param name="point1">Triangle point position using boundingbox bottomleft as origin</param>
        /// <param name="point2">Triangle point position using boundingbox bottomleft as origin</param>
        /// <param name="point3">Triangle point position using boundingbox bottomleft as origin</param>
        public static TrianglePosition GetTrianglePosition(Vector3 point1, Vector3 point2, Vector3 point3, Bounds boundingBox)
        {
            int counter = 0;
            if (PointIsInsideArea(point1, boundingBox)) { counter++; }
            if (PointIsInsideArea(point2, boundingBox)) { counter++; }
            if (PointIsInsideArea(point3, boundingBox)) { counter++; }

            if (counter == 0)
            {
                Vector3 bbox1 = boundingBox.min;
                Vector3 bbox2 = new(boundingBox.max.x, boundingBox.min.y, boundingBox.min.z);
                Vector3 bbox3 = boundingBox.max;
                Vector3 bbox4 = new(boundingBox.min.x, boundingBox.max.y, boundingBox.max.z);

                if (PointIsInTriangle(bbox1, point1, point2, point3)) return TrianglePosition.overlap;
                if (PointIsInTriangle(bbox2, point1, point2, point3)) return TrianglePosition.overlap;
                if (PointIsInTriangle(bbox3, point1, point2, point3)) return TrianglePosition.overlap;
                if (PointIsInTriangle(bbox4, point1, point2, point3)) return TrianglePosition.overlap;

                return TrianglePosition.outside;
            }
            else if (counter == 3)
            {
                return TrianglePosition.inside;
            }
            else
            {
                return TrianglePosition.overlap;
            }
        }

        public static bool PointIsInTriangle(Vector3 p, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            var a = .5f * (-p1.z * p2.x + p0.z * (-p1.x + p2.x) + p0.x * (p1.z - p2.z) + p1.x * p2.z);
            var sign = a < 0 ? -1 : 1;
            var s = (p0.z * p2.x - p0.x * p2.z + (p2.z - p0.z) * p.x + (p0.x - p2.x) * p.z) * sign;
            var t = (p0.x * p1.z - p0.z * p1.x + (p0.z - p1.z) * p.x + (p1.x - p0.x) * p.z) * sign;

            return s > 0 && t > 0 && (s + t) < 2 * a * sign;
        }

        public static bool PointIsInsideArea(Vector3 vector, Bounds boundingBox)
        {
            return boundingBox.Contains(vector);
        }

        private void ReadVertices()
        {
            Vector3[] verts = sourceMesh.vertices;
            vertexWorldPositions = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                vertexWorldPositions[i] = verts[i] + sourceOrigin;
            }
        }

        public enum TrianglePosition
        {
            outside,
            overlap,
            inside
        }
    }
}