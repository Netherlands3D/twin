using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class PolygonSelectionCalculator : MonoBehaviour
    {
        public static List<LayerData> Layers = new();
        private PointerToWorldPosition pointerToWorldPosition;

        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }

        private void OnEnable()
        {
            ClickNothingPlane.ClickedOnNothing.AddListener(ProcessClick);
        }

        private void OnDisable()
        {
            ClickNothingPlane.ClickedOnNothing.RemoveListener(ProcessClick);
        }

        public static void RegisterPolygon(LayerData layer)
        {
            if (Layers.Contains(layer))
            {
                Debug.LogError("layer " + layer + " is already registered");
                return;
            }

            Layers.Add(layer);
        }

        public static void UnregisterPolygon(LayerData layer)
        {
            if (!Layers.Remove(layer))
                Debug.LogError("layer " + layer + " is not registered");
        }

        private void ProcessClick()
        {
            var camera = Camera.main;
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            var worldPoint = pointerToWorldPosition.WorldPoint.ToUnity();

            foreach (var layer in Layers)
            {               
                bool wasSelected = ProcessPolygonSelection(layer, camera, frustumPlanes, worldPoint);
                if (wasSelected)
                {
                    layer.SelectLayer(true);
                    return; //select only one
                }
                else
                {
                    layer.DeselectLayer(); //deselect if the click wasn't in the polygon and the multiselect modifier keys aren't pressed
                }
            }
        }

        private bool ProcessPolygonSelection(LayerData layer, Camera camera, Plane[] frustumPlanes, Vector3 worldPoint)
        {
            //since we use a visual projection of the polygon, we need to calculate if a user clicks on the polygon manually
            //if this polygon is out of view of the camera, it can't be clicked on.
            
            var polygonPropertyData = layer.GetProperty<PolygonSelectionLayerPropertyData>();
            if(polygonPropertyData == null || polygonPropertyData.OriginalPolygon == null ||  polygonPropertyData.OriginalPolygon.Count == 0)
                return false;
            
            var bbox = new BoundingBox(polygonPropertyData.OriginalPolygon[0], polygonPropertyData.OriginalPolygon[0]);
            for (var i = 1; i < polygonPropertyData.OriginalPolygon.Count; i++)
            {
                var coord = polygonPropertyData.OriginalPolygon[i];
                bbox.Encapsulate(coord);
            }

            var bounds = bbox.ToUnityBounds();
            
            if (!IsBoundsInView(bounds, frustumPlanes))
                return false;

            //if the click is outside of the polygon bounds, this polygon wasn't selected
            var point2d = new Vector2(worldPoint.x, worldPoint.z);
            if (!IsInBounds2D(bounds, point2d))
                return false;

            //check if the click was in the polygon bounds
            var vertices = PolygonUtility.CoordinatesToVertices(polygonPropertyData.OriginalPolygon, polygonPropertyData.LineWidth);
            var polygon = new CompoundPolygon(vertices);
            return CompoundPolygon.IsPointInPolygon(point2d, polygon);
        }

        public static bool IsBoundsInView(Bounds bounds, Plane[] frustumPlanes)
        {
            return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
        }

        public static bool IsInBounds2D(Bounds bounds, Vector2 point)
        {
            return point.x > bounds.min.x && point.x < bounds.max.x && point.y > bounds.min.z && point.y < bounds.max.z;
        }
    }
}