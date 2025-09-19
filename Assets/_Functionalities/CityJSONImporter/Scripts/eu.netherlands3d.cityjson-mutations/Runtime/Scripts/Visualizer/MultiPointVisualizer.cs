using System;
using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.CityJson.Visualisation
{
    public class MultiPointVisualizer : CityObjectVisualizer
    {
        [SerializeField] private GameObject visualizationObject;

        protected override List<BoundaryMeshData> BoundariesToMeshes(CityBoundary boundary, CoordinateSystem coordinateSystem, Vector3Double origin)
        {
            if (!(boundary is CityMultiPoint))
                throw new NotSupportedException("Boundary is not of Type MultiPoint, use CityObjectVisualiser instead.");

            return PointsToMeshes(boundary as CityMultiPoint, coordinateSystem, visualizationObject, origin);
        }

        private List<BoundaryMeshData> PointsToMeshes(CityMultiPoint boundary, CoordinateSystem coordinateSystem, GameObject visualizationObject, Vector3Double origin)
        {
            var meshes = new List<BoundaryMeshData>();
            var verts = GetConvertedPolygonVertices(boundary.Points, coordinateSystem, origin);
            for (int i = 0; i < boundary.VertexCount; i++)
            {
                CityGeometrySemanticsObject semantics = null;
                if (boundary.SemanticsObjects.Count > 0)
                    semantics = boundary.SemanticsObjects[i];

                var mesh = InstantiateObjectAtPoint(verts[i], coordinateSystem, visualizationObject);
                // meshes.Add(new BoundaryMeshData(mesh, semantics));
                throw new NotImplementedException("Due to the separation of triangulation and mesh combination, this class is no longer able to visualize MultiPoint geometries");
            }

            return meshes;
        }

        private Mesh InstantiateObjectAtPoint(Vector3 point, CoordinateSystem coordinateSystem, GameObject visualizationObject)
        {
            var obj = Instantiate(visualizationObject, point, Quaternion.identity, transform);
            var meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter)
                return meshFilter.mesh;

            return null;
        }
    }
}