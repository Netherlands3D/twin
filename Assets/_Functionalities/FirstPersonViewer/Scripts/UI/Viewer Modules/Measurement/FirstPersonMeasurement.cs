using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Samplers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurement : MonoBehaviour
    {
        private const float POINT_DELETE_DISTANCE = 1f;

        private OpticalRaycaster raycaster;
        
        public List<FirstPersonMeasurementSegment> Segments => measurementSegments;

        private List<FirstPersonMeasurementSegment> measurementSegments = new();

        [Header("Measuring")]
        [SerializeField] private InputActionReference mouseClick;
        [SerializeField] private InputActionReference measuringYLockModifier;
        [SerializeField] private InputActionReference measuringXZLockModifier;
        [SerializeField] private LayerMask measurementLayerMask;
        [SerializeField] private float maxClickDuration = 0.1f;
        
        private float LastClickTime;
        private Vector2 clickPosition;

        [SerializeField] private FirstPersonMeasurementPoint pointObject;
        [SerializeField] private float textHeightAboveLines = .65f;

        [Header("UI")]
        [SerializeField] private FirstPersonMeasurementElement measurementElementPrefab;

        [SerializeField] private FirstPersonMeasurementSegment measurementSegmentPrefab;
        [SerializeField] private Transform measurementUIParent;
        private Transform measurementWorldParent;

        [SerializeField] private Color[] lineColors;
        [SerializeField] private TextMeshProUGUI totalDistanceText;
        private float totalDistanceInMeters;
        private bool calculateDistanceDirty = true;

        private Action<Vector3, bool> setMeasurementPointCallback;
        private Action<Vector3, bool> removeMeasurementPointCallback;
        private Action<FirstPersonMeasurementElement> removeElementCallback;
        
        

        private void Start()
        {
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            setMeasurementPointCallback = SetMeasuringPoint;
            removeMeasurementPointCallback = RemoveMeasuringPoint;
            removeElementCallback = OnElementRemoved;
            
            GameObject measurementParentObject = new GameObject("MeasurementToolParent");
            measurementWorldParent = measurementParentObject.transform;
        }

        private void OnDisable()
        {
            ResetMeasurements();
        }

        private void Update()
        {
            if (calculateDistanceDirty)
            {
                calculateDistanceDirty = false;
                RecalculateTotalDistance();
                // Refresh remaining segments
                for (int i = 0; i < measurementSegments.Count; i++)
                    measurementSegments[i].Refresh(i, lineColors[(i + 1) % lineColors.Length]);
            }

            HandleInput();
        }

        public void HandleInput()
        {
            if (Interface.PointerIsOverUI()) return;
            
            if (mouseClick.action.WasPressedThisFrame())
            {
                 clickPosition = Pointer.current.position.ReadValue();
                 float heldTime = Time.time - LastClickTime;
                 if (heldTime > maxClickDuration)
                    raycaster.GetWorldPointAsync(clickPosition, setMeasurementPointCallback, App.Cameras.ActiveCamera, measurementLayerMask);

                 LastClickTime = Time.time;
            }
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if(measurementSegments.Count > 0)
                    raycaster.GetWorldPointAsync(Pointer.current.position.ReadValue(), removeMeasurementPointCallback, App.Cameras.ActiveCamera, measurementLayerMask);
            }
        }

        private void RecalculateTotalDistance()
        {
            totalDistanceInMeters = 0;
            for (int i = 0; i < measurementSegments.Count; i++)
                totalDistanceInMeters += measurementSegments[i].LineDistance;

            totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);
        }

        private void SetMeasuringPoint(Vector3 point, bool hit)
        {
            if (!hit) return;

            point = CheckForModifiersPressed(point);

            FirstPersonMeasurementPoint newPoint = Instantiate(pointObject, point, Quaternion.identity);
            newPoint.transform.SetParent(measurementWorldParent);
            if (measurementSegments.Count > 0)
            {
                Color objectColor = lineColors[measurementSegments.Count % lineColors.Length];
                FirstPersonMeasurementSegment prevSegment = measurementSegments[^1];
                prevSegment.SetSecondPoint(newPoint, objectColor);
            }
            
            FirstPersonMeasurementSegment measurementSegment = Instantiate(measurementSegmentPrefab, point, Quaternion.identity);
            measurementSegment.SetFirstPoint(newPoint, measurementSegments.Count);
            measurementSegment.transform.SetParent(measurementWorldParent);

            FirstPersonMeasurementElement measurementElement = Instantiate(measurementElementPrefab, measurementUIParent);
            measurementElement.transform.SetSiblingIndex(measurementUIParent.childCount - 2);

            measurementSegment.SetElement(measurementElement, measurementSegments.Count, removeElementCallback);

            measurementSegments.Add(measurementSegment);
            calculateDistanceDirty = true;
        }

        private Vector3 CheckForModifiersPressed(Vector3 position)
        {
            if (measurementSegments.Count == 0)
                return position;

            FirstPersonMeasurementSegment lastSegment = measurementSegments[^1];
            Vector3 lastPoint = lastSegment.GetLastPosition();

            // Y-lock
            if (measuringYLockModifier.action.IsPressed())
                position.y = lastPoint.y;

            // XZ-lock
            if (measuringXZLockModifier.action.IsPressed())
            {
                position.x = lastPoint.x;
                position.z = lastPoint.z;
            }

            return position;
        }

        public void OnElementRemoved(FirstPersonMeasurementElement element)
        {
            int index = measurementSegments.FindIndex(s => s.Element == element);
            if (index == -1) return;

            RemoveSegmentAt(index + 1);
        }
        
        private void RemoveSegmentAt(int index)
        {
            if(index >= measurementSegments.Count) return;
            
            FirstPersonMeasurementSegment segment = measurementSegments[index];
            if (index > 0)
            { 
                measurementSegments[index - 1].SetSecondPoint(segment.PointB, lineColors[measurementSegments.Count % lineColors.Length]);
            }

            segment.RemoveFirstPoint();
            measurementSegments.RemoveAt(index);
            Destroy(segment.gameObject);
            calculateDistanceDirty = true;
        }

        private void RemoveMeasuringPoint(Vector3 point, bool hit)
        {
            if (!hit) return;

            for (int i = measurementSegments.Count - 1; i >= 0; i--)
            {
                FirstPersonMeasurementSegment segment = measurementSegments[i];
                bool removeA = segment.PointA != null && IsPointClose(point, segment.PointA.transform.position, POINT_DELETE_DISTANCE);
                bool removeB = segment.PointB != null && IsPointClose(point, segment.PointB.transform.position, POINT_DELETE_DISTANCE);
                
                if (removeA)
                {
                    RemoveSegmentAt(i);
                    break;
                }
            }
        }

        private bool IsPointClose(Vector3 a, Vector3 b, float distance) => (a - b).sqrMagnitude <= distance * distance;

        public void ResetMeasurements()
        {
            foreach (FirstPersonMeasurementSegment segment in measurementSegments)
            {
                segment.Remove();
            }       
            measurementSegments.Clear();
            calculateDistanceDirty = true;
        }

        public static string ConvertToUnits(float valueInMeters)
        {
            if (valueInMeters >= 1000f)
                return $"~{(valueInMeters / 1000f):F2}km";
    
            return $"~{valueInMeters:F2}m";
        }
    }
}
