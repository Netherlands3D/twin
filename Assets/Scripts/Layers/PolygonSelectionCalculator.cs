using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin
{
    public class PolygonSelectionCalculator : MonoBehaviour
    {
        public static List<PolygonSelectionLayer> Layers = new();
        private OpticalRaycaster opticalRaycaster;
        private static bool polygonAddedThisFrame;

        private void Awake()
        {
            opticalRaycaster = FindAnyObjectByType<OpticalRaycaster>();
        }

        private void OnEnable()
        {
            ClickNothingPlane.ClickedOnNothing.AddListener(ProcessClick);
        }

        private void OnDisable()
        {
            ClickNothingPlane.ClickedOnNothing.RemoveListener(ProcessClick);
        }

        public static void RegisterPolygon(PolygonSelectionLayer layer)
        {
            if (Layers.Contains(layer))
            {
                Debug.LogError("layer " + layer + " is already registered");
                return;
            }

            Layers.Add(layer);
            polygonAddedThisFrame = true;
        }

        public static void UnregisterPolygon(PolygonSelectionLayer layer)
        {
            if (!Layers.Remove(layer))
                Debug.LogError("layer " + layer + " is not registered");
        }

        private void ProcessClick()
        {
            if (polygonAddedThisFrame) //don't immediately deselect a just created polygon
            {
                polygonAddedThisFrame = false;
                return;
            }

            var camera = Camera.main;
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            var worldPoint = opticalRaycaster.WorldPoint;

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

        private bool ProcessPolygonSelection(PolygonSelectionLayer layer, Camera camera, Plane[] frustumPlanes, Vector3 worldPoint)
        {
            //since we use a visual projection of the polygon, we need to calculate if a user clicks on the polygon manually
            //if this polygon is out of view of the camera, it can't be clicked on.
            var bounds = layer.Polygon.Bounds;
            if (!IsBoundsInView(bounds, frustumPlanes))
                return false;

            //if the click is outside of the polygon bounds, this polygon wasn't selected
            var point2d = new Vector2(worldPoint.x, worldPoint.z);
            if (!IsInBounds2D(bounds, point2d))
                return false;

            //check if the click was in the polygon bounds
            return CompoundPolygon.IsPointInPolygon(point2d, layer.Polygon);
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