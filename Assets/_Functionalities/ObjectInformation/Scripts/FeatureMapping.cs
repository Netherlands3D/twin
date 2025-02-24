using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class FeatureMapping : IMapping
    {
        public string DebugID;

        public object MappingObject => feature;
        public string FeatureID => feature.Id;
        public IGeoJsonVisualisationLayer VisualisationLayer { get { return visualisationLayer; } }
        public GeoJsonLayerGameObject VisualisationParent { get { return geoJsonLayerParent; } }
        public List<Mesh> FeatureMeshes { get { return visualisationLayer.GetMeshData(feature); } }
        public Feature Feature { get { return feature; } }
        public int LayerOrder { get { return geoJsonLayerParent.LayerData.RootIndex; } }
        public BoundingBox BoundingBox => boundingBox;

        private Feature feature;
        private List<Mesh> meshes;
        private IGeoJsonVisualisationLayer visualisationLayer;
        private GeoJsonLayerGameObject geoJsonLayerParent;
        private BoundingBox boundingBox;

        public void SetGeoJsonLayerParent(GeoJsonLayerGameObject parentLayer)
        {
            geoJsonLayerParent = parentLayer;
        }

        public void SetFeature(Feature feature)
        {
            this.feature = feature; 
            
            DebugID = feature.Id;
        }

        public void SetMeshes(List<Mesh> meshes)
        {
            this.meshes = meshes;       
        }

        public void SetVisualisationLayer(IGeoJsonVisualisationLayer visualisationLayer)
        {
            this.visualisationLayer = visualisationLayer;
            
        }

        /// <summary>
        /// calculating a hit on the XZ plane
        /// </summary>
        /// <param name="position"></param>
        /// <param name="range"></param>
        /// <param name="hitPoint"></param>
        /// <returns></returns>
        public bool IsPositionHit(Coordinate position, float range, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            List<Mesh> meshes = FeatureMeshes;
            Vector3 unityPos = position.ToUnity();
            Vector2 xzPos = new Vector2(unityPos.x, unityPos.z);
            for(int i = 0; i < meshes.Count; i++)
            {
                if (feature.Geometry is MultiLineString || feature.Geometry is LineString)
                {
                    Vector3[] vertices = meshes[i].vertices;
                    for (int j = 0; j < meshes[i].vertexCount - 1; j++)
                    {
                        Vector3 xzStart = new Vector3(vertices[j].x, unityPos.y, vertices[j].z);
                        Vector3 xzEnd = new Vector3(vertices[j + 1].x, unityPos.y, vertices[j + 1].z);
                        Vector3 nearest = FeatureSelector.NearestPointOnFiniteLine(xzStart, xzEnd, unityPos);
                        float dist = Vector2.SqrMagnitude(xzPos - new Vector2(nearest.x, nearest.z));
                        if (dist < range * range)
                        {
                            hitPoint = nearest;
                            return true;
                        }
                    }
                }
                else if (feature.Geometry is MultiPolygon || feature.Geometry is Polygon)
                {
                    Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
                    GeoJSONPolygonLayer polygonLayer = visualisationLayer as GeoJSONPolygonLayer;
                    PolygonVisualisation pv = polygonLayer.GetPolygonVisualisationByMesh(meshes);
                    bool isSelected = FeatureSelector.ProcessPolygonSelection(meshes[i], pv.transform, frustumPlanes, unityPos);
                    if (isSelected)
                    {
                        hitPoint = unityPos;
                        return true;
                    }
                }
                else if (feature.Geometry is Point || feature.Geometry is MultiPoint)
                {
                    Vector3[] vertices = meshes[i].vertices;
                    for (int j = 0; j < meshes[i].vertexCount; j++)
                    {
                        Vector2 xzVertex = new Vector2(vertices[j].x, vertices[j].z);
                        float dist = Vector2.SqrMagnitude(xzPos - xzVertex);
                        if (dist < range * range)
                        {
                            hitPoint = new Vector3(xzVertex.x, unityPos.y, xzVertex.y);
                            return true;
                        }
                    }
                }
            }            
            return false;
        }

        //TODO maybe this should be automated and called within the set visualisation layer
        public void UpdateBoundingBox()
        {
            if (feature == null)
            {
                Debug.LogError("must have feature for boundingbox");
                return;
            }
            if (visualisationLayer == null)
            {
                Debug.LogError("must have a geojson visualisation layer for feature");
                return;
            }

            boundingBox = CreateBoundingBoxForFeature(feature, visualisationLayer);
        }

        public static BoundingBox CreateBoundingBoxForFeature(Feature feature, IGeoJsonVisualisationLayer layer)
        {
            Bounds featureBounds = layer.GetFeatureBounds(feature);
            Coordinate bottomLeft = new Coordinate(CoordinateSystem.Unity, featureBounds.min.x, featureBounds.min.y, featureBounds.min.z);
            Coordinate topRight = new Coordinate(CoordinateSystem.Unity, featureBounds.max.x, featureBounds.max.y, featureBounds.max.z);
            Coordinate blWgs84 = bottomLeft.Convert(CoordinateSystem.WGS84_LatLon);
            Coordinate trWgs84 = topRight.Convert(CoordinateSystem.WGS84_LatLon);
            BoundingBox boundingBox = new BoundingBox(blWgs84, trWgs84);
            return boundingBox;
        }

        //these gameobjects can represent selected features or thumbnail meshes
        private List<GameObject> CreateFeatureGameObjects()
        {
            //polygons are already visible and need no new subobjects to be rendered
            List<GameObject> subObjects = new List<GameObject>();
            for (int i = 0; i < meshes.Count; i++)
            {
                Mesh mesh = meshes[i];
                Vector3[] verts = mesh.vertices;
                float width = 1f;
                GameObject subObject = new GameObject(feature.Geometry.ToString() + "_submesh_" + visualisationLayer.Transform.transform.childCount.ToString());
                subObject.AddComponent<MeshFilter>().mesh = mesh;
                if (verts.Length >= 2)
                {
                    //generate collider extruded lines for lines
                    if (feature.Geometry is MultiLineString || feature.Geometry is LineString)
                    {
                        GeoJSONLineLayer lineLayer = visualisationLayer as GeoJSONLineLayer;
                        width = lineLayer.LineRenderer3D.LineDiameter;
                        float halfWidth = width * 0.5f;

                        int segmentCount = verts.Length - 1;
                        int vertexCount = segmentCount * 4;  // 4 vertices per segment
                        int triangleCount = segmentCount * 6; // 2 triangles per segment, 3 vertices each

                        Vector3[] vertices = new Vector3[vertexCount];
                        int[] triangles = new int[triangleCount];

                        for (int j = 0; j < segmentCount; j++)
                        {
                            Vector3 p1 = verts[j];
                            Vector3 p2 = verts[j + 1];
                            Vector3 edgeDir = (p2 - p1).normalized;
                            Vector3 perpDir = new Vector3(edgeDir.z, 0, -edgeDir.x);

                            Vector3 v1 = p1 + perpDir * halfWidth;
                            Vector3 v2 = p1 - perpDir * halfWidth;
                            Vector3 v3 = p2 + perpDir * halfWidth;
                            Vector3 v4 = p2 - perpDir * halfWidth;

                            int baseIndex = j * 4;
                            vertices[baseIndex + 0] = v1; // Top left
                            vertices[baseIndex + 1] = v2; // Bottom left
                            vertices[baseIndex + 2] = v3; // Top right
                            vertices[baseIndex + 3] = v4; // Bottom right

                            int triBaseIndex = j * 6;
                            // Triangle 1
                            triangles[triBaseIndex + 0] = baseIndex + 0;
                            triangles[triBaseIndex + 1] = baseIndex + 1;
                            triangles[triBaseIndex + 2] = baseIndex + 2;

                            // Triangle 2
                            triangles[triBaseIndex + 3] = baseIndex + 2;
                            triangles[triBaseIndex + 4] = baseIndex + 1;
                            triangles[triBaseIndex + 5] = baseIndex + 3;
                        }
                        mesh.vertices = vertices.ToArray();
                        mesh.triangles = triangles.ToArray();
                        subObject.AddComponent<MeshRenderer>().material = lineLayer.LineRenderer3D.LineMaterial;
                    }
                }
                else
                {
                    if (feature.Geometry is Point || feature.Geometry is MultiPoint)
                    {
                        int segments = 12;
                        float radius = ((GeoJSONPointLayer)VisualisationLayer).PointRenderer3D.MeshScale;
                        subObject.transform.position = verts[0];
                        Vector3 centerVertex = Vector3.zero;
                        Vector3[] vertices = new Vector3[segments + 1];
                        int[] triangles = new int[segments * 3];
                        float angleIncrement = 360.0f / segments;
                        for (int j = 0; j < segments; j++)
                        {
                            float angle = Mathf.Deg2Rad * (j * angleIncrement);
                            float x = Mathf.Cos(angle) * radius;
                            float z = Mathf.Sin(angle) * radius;
                            vertices[j + 1] = new Vector3(centerVertex.x + x, centerVertex.y, centerVertex.z + z);
                            triangles[j * 3] = 0;
                            triangles[j * 3 + 1] = (j + 2 > segments) ? 1 : j + 2;
                            triangles[j * 3 + 2] = j + 1;
                        }
                        mesh.vertices = vertices.ToArray();
                        mesh.triangles = triangles.ToArray();
                        subObject.AddComponent<MeshRenderer>().material = ((GeoJSONPointLayer)VisualisationLayer).PointRenderer3D.Material;
                    }
                }

                mesh.RecalculateBounds();
                meshes[i] = mesh;

                subObject.transform.SetParent(visualisationLayer.Transform);
                subObject.layer = LayerMask.NameToLayer("Projected");
                subObjects.Add(subObject);
            }
            return subObjects;
        }

        public GameObject SelectedGameObject => selectedGameObjects.FirstOrDefault();

        private List<GameObject> selectedGameObjects = new List<GameObject>();

        public void SelectFeature()
        {
            //transform for mesh world matrix
            selectedGameObjects = CreateFeatureGameObjects();
            if (selectedGameObjects.Count == 0) return; 

            Color selectionColor = Color.blue;
            visualisationLayer.SetVisualisationColor(selectedGameObjects[0].transform, meshes, selectionColor);
            foreach (Mesh mesh in meshes)
            {
                Color[] colors = new Color[mesh.vertexCount];
                for (int i = 0; i < mesh.vertexCount; i++)
                    colors[i] = THUMBNAIL_COLOR;
                mesh.SetColors(colors);
            }
        }

        public void DeselectFeature()
        {
            if(selectedGameObjects.Count > 0)
            {
                for (int i = selectedGameObjects.Count - 1; i >= 0; i--)
                    GameObject.Destroy(selectedGameObjects[i]);
            }
            selectedGameObjects.Clear();

            visualisationLayer.SetVisualisationColorToDefault();
            foreach (Mesh mesh in meshes)
            {
                Color[] colors = new Color[mesh.vertexCount];
                for (int i = 0; i < mesh.vertexCount; i++)
                    colors[i] = NO_OVERRIDE_COLOR;
                mesh.SetColors(colors);
            }
        }

        private static readonly Color NO_OVERRIDE_COLOR = new Color(0, 0, 1, 0);
        private static readonly Color THUMBNAIL_COLOR = new Color(1, 0, 0, 0);
    }
}
