using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Utility;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class FeatureSelector : MonoBehaviour, IObjectSelector
    {      
        
        public bool HasFeatureMapping => featureMappings.Count > 0;
        public bool HasPolygons
        {
            get
            {
                return featureMappings
                    .SelectMany(entry => entry.Value)
                    .Any(featureMapping => featureMapping.VisualisationLayer.IsPolygon);
            }
        }
        
        public List<FeatureMapping> FeatureMappings => featureMappings.SelectMany(entry => entry.Value).ToList();

        
        private GameObject testHitPosition;
        private GameObject testGroundPosition;
        private Dictionary<GeoJsonLayerGameObject, List<FeatureMapping>> featureMappings = new();
        private Camera mainCamera;       
        private ObjectMapping blockingObjectMapping;
        private Vector3 blockingObjectMappingHitPoint;
        private PointerToWorldPosition pointerToWorldPosition;
        private MappingTree mappingTreeInstance;

        private void Awake()
        {
            mainCamera = Camera.main;
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }

        public void SetMappingTree(MappingTree mappingTree)
        {
            mappingTreeInstance = mappingTree;
        }

        public void Select(FeatureMapping mapping)
        {
            mapping.SelectFeature();
        }

        public void Deselect()
        {
            if (featureMappings.Count > 0)
            {
                foreach (var mapping in featureMappings.SelectMany(pair => pair.Value))
                {
                    mapping.DeselectFeature();
                }
            }
        }

        //in case an objectmappaing was already selected it should be handled in the feature selection too
        public void SetBlockingObjectMapping(ObjectMapping mapping, Vector3 blockingObjectMappingHitPoint)
        {
            blockingObjectMapping = mapping;
            this.blockingObjectMappingHitPoint = blockingObjectMappingHitPoint;
        }

        public void FindFeatureByPosition(Vector3 position)
        {
            featureMappings.Clear();


            if (blockingObjectMapping != null)
            {
                Coordinate blockingCoordinate = new Coordinate(blockingObjectMappingHitPoint);
                List<IMapping> potentialMappingsUnderBlockingObject = mappingTreeInstance.QueryMappingsContainingNode<FeatureMapping>(blockingCoordinate);
                foreach (FeatureMapping map in potentialMappingsUnderBlockingObject)
                {
                    Vector3 hitPoint;
                    if (map.IsPositionHit(blockingCoordinate, map.VisualisationLayer.GetSelectionRange(), out hitPoint))
                    {
                        position = hitPoint;

                    }
                }
            }
            //please dont remove as this can be very useful to check where the user clicked
            //ShowFeatureDebuggingIndicator(groundPosition);

            Coordinate targetCoordinate = new Coordinate(position);
            List<IMapping> foundMappings = mappingTreeInstance.QueryMappingsContainingNode<FeatureMapping>(targetCoordinate);
            foreach (FeatureMapping map in foundMappings)
            {
                Vector3 hitPoint;
                if (map.IsPositionHit(targetCoordinate, map.VisualisationLayer.GetSelectionRange(), out hitPoint))
                {
                    featureMappings.TryAdd(map.VisualisationParent, new List<FeatureMapping>());
                    featureMappings[map.VisualisationParent].Add(map);
                }
            }
        }

        public void FindFeatureAtPointerPosition()
        {            
            Vector3 groundPosition = pointerToWorldPosition.WorldPoint;
            FindFeatureByPosition(groundPosition);            
        }

        private void ShowFeatureDebuggingIndicator(Vector3 groundPosition)
        {
            if (testHitPosition == null)
            {
                testHitPosition = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                testHitPosition.transform.localScale = Vector3.one * 3;

                testGroundPosition = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                testGroundPosition.transform.localScale = Vector3.one * 2.5f;
                testGroundPosition.GetComponent<MeshRenderer>().material.color = Color.red;
            }

            testGroundPosition.transform.position = groundPosition + Vector3.up * 5f;
        }

        public static bool ProcessPolygonSelection(Mesh polygon, Transform transform, Plane[] frustumPlanes, Vector3 worldPoint)
        {
            Bounds localBounds = polygon.bounds;
            Matrix4x4 localToWorld = transform.localToWorldMatrix;
            Vector3 worldCenter = localToWorld.MultiplyPoint3x4(localBounds.center);
            Bounds worldBounds = new Bounds(worldCenter, polygon.bounds.size);

            if (!PolygonSelectionCalculator.IsBoundsInView(worldBounds, frustumPlanes))
                return false;

            var point2d = new Vector2(worldPoint.x, worldPoint.z);
            if (!PolygonSelectionCalculator.IsInBounds2D(worldBounds, point2d))
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
    }    
}