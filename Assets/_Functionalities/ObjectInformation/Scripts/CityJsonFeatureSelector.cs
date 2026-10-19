using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Structure;
using UnityEngine;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class CityJsonFeatureSelector : MonoBehaviour, IObjectSelector
    {      
        public bool HasFeatureMapping => featureMappings.Count > 0;
        public List<CityJSONMapping> FeatureMappings => featureMappings.SelectMany(entry => entry.Value).ToList();
        
        private GameObject testHitPosition;
        private GameObject testGroundPosition;
        private Dictionary<CityJSONLayerGameObject, List<CityJSONMapping>> featureMappings = new();
        private Vector3 blockingObjectMappingHitPoint;
        private ObjectMapping blockingObjectMapping;
        private PointerToWorldPosition pointerToWorldPosition;

        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }

        public void Select(CityJSONMapping mapping)
        {
            mapping.Select(null);
        }

        public void Deselect()
        {
            if (featureMappings.Count > 0)
            {
                foreach (var mapping in featureMappings.SelectMany(pair => pair.Value))
                {
                    mapping.Deselect();
                }
            }
        }

        public void FindFeatureByPosition(Vector3 position)
        {
            featureMappings.Clear();
            Coordinate targetCoordinate = new Coordinate(position);
            List<IMapping> foundMappings = SelectionService.MappingTree.QueryMappingsContainingNode<CityJSONMapping>(targetCoordinate);
            foreach (CityJSONMapping map in foundMappings)
            {
                if (map.IsPositionHit(position))
                {
                    featureMappings.TryAdd(map.VisualisationParent, new List<CityJSONMapping>());
                    featureMappings[map.VisualisationParent].Add(map);
                }
            }
        }

        public void FindFeatureAtPointerPosition()
        {            
            Vector3 groundPosition = pointerToWorldPosition.GetWorldPointUsingOpticalRaycaster();
            FindFeatureByPosition(groundPosition);            
        }

        public static bool ProcessPolygonSelection(Mesh polygon, Transform transform, Plane[] frustumPlanes, Vector3 worldPoint)
        {
            Bounds localBounds = polygon.bounds;
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            Vector3 worldCenter = localToWorld.MultiplyPoint3x4(localBounds.center);
            Bounds worldBounds = new Bounds(worldCenter, polygon.bounds.size);

            if (!PolygonSelectionService.IsBoundsInView(worldBounds, frustumPlanes))
                return false;

            var point2d = new Vector2(worldPoint.x, worldPoint.z);
            if (!PolygonSelectionService.IsInBounds2D(worldBounds, point2d))
                return false;

            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Vector3 localPosition = worldToLocal.MultiplyPoint(worldPoint);

            return IsPointInMesh(polygon, localPosition);
        }

        public static bool IsPointInMesh(Mesh mesh, Vector3 point)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3 projectedPoint = new Vector3(point.x, 0, point.z);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = new Vector3(vertices[triangles[i]].x, 0, vertices[triangles[i]].z);
                Vector3 v1 = new Vector3(vertices[triangles[i + 1]].x, 0, vertices[triangles[i + 1]].z);
                Vector3 v2 = new Vector3(vertices[triangles[i + 2]].x, 0, vertices[triangles[i + 2]].z);
                if (ContainsPointProjected2D(new List<Vector3> { v0, v1, v2 }, projectedPoint))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a 2d polygon contains point p
        /// </summary>
        /// <param name="polygon">array of points that define the polygon</param>
        /// <param name="p">point to test</param>
        /// <returns>true if point p is inside the polygon, otherwise false</returns>
        public static bool ContainsPointProjected2D(IList<Vector3> polygon, Vector3 p)
        {
            var j = polygon.Count - 1;
            var inside = false;
            for (int i = 0; i < polygon.Count; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                if (((pi.z <= p.z && p.z < pj.z) || (pj.z <= p.z && p.z < pi.z)) &&
                    (p.x < (pj.x - pi.x) * (p.z - pi.z) / (pj.z - pi.z) + pi.x))
                    inside = !inside;
            }
            return inside;
        }

        public static Vector3 NearestPointOnFiniteLine(Vector3 start, Vector3 end, Vector3 pnt)
        {
            var line = (end - start);
            var len = line.magnitude;
            line.Normalize();

            var v = pnt - start;
            var d = Vector3.Dot(v, line);
            d = Mathf.Clamp(d, 0f, len);
            return start + line * d;
        }

        public void TryAddMappings(Dictionary<IMapping, int> mappings, bool filterDuplicateFeatures)
        {
            if (HasFeatureMapping)
            {
                List<CityObject> filterDuplicates = new List<CityObject>();
                foreach (CityJSONMapping feature in FeatureMappings)
                {
                    if (feature.VisualisationParent.LayerData.ActiveInHierarchy)
                    {
                        if (filterDuplicateFeatures)
                        {
                            if (!filterDuplicates.Contains(feature.Feature))
                                filterDuplicates.Add(feature.Feature);
                            else
                                continue;
                        }
                        mappings.TryAdd(feature, feature.VisualisationParent.LayerData.RootId);
                    }
                }
            }
        }
    }    
}