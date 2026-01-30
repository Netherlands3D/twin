using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Samplers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurement : MonoBehaviour
    {
        private const float POINT_DELETE_DISTANCE = 1f;

        private OpticalRaycaster raycaster;

        private List<FirstPersonMeasurementSegment> measurementSegments = new List<FirstPersonMeasurementSegment>();

        [Header("Measuring")]
        [SerializeField] private InputActionReference mouseClick;
        [SerializeField] private InputActionReference measuringYLockModifier;
        [SerializeField] private InputActionReference measuringXZLockModifier;
        [SerializeField] private LayerMask measurementLayerMask;
        [SerializeField] private float maxClickDuration = 0.1f;
        private float clickDownTime;
        private Vector2 clickPosition;

        [SerializeField] private FirstPersonMeasurementPoint pointObject;
        [SerializeField] private float textHeightAboveLines = .65f;

        [Header("UI")]
        [SerializeField] private FirstPersonMeasurementElement measurementElementPrefab;
        [SerializeField] private Transform measurementParent;

        [SerializeField] private Color32[] lineColors;
        [SerializeField] private TextMeshProUGUI totalDistanceText;
        private float totalDistanceInMeters;

        private Action<Vector3, bool> setMeasurementPointCallback;
        private Action<Vector3, bool> removeMeasurementPointCallback;
        private Action<FirstPersonMeasurementElement> removeElementCallback;

        private void Start()
        {
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            setMeasurementPointCallback = SetMeasuringPoint;
            removeMeasurementPointCallback = RemoveMeasuringPoint;
            removeElementCallback = OnElementRemoved;
        }

        private void OnDisable()
        {
            ResetMeasurements();
        }

        private void Update()
        {
            if (mouseClick.action.WasPressedThisFrame() && !Interface.PointerIsOverUI())
            {
                clickPosition = Pointer.current.position.ReadValue();
                clickDownTime = Time.time;
            }

            if (mouseClick.action.WasReleasedThisFrame())
            {
                float heldTime = Time.time - clickDownTime;

                if (heldTime <= maxClickDuration)
                    raycaster.GetWorldPointAsync(clickPosition, setMeasurementPointCallback, App.Cameras.ActiveCamera, measurementLayerMask);
            }

            if (Mouse.current.rightButton.wasPressedThisFrame && measurementSegments.Count > 0)
                raycaster.GetWorldPointAsync(Pointer.current.position.ReadValue(), removeMeasurementPointCallback, App.Cameras.ActiveCamera, measurementLayerMask);
        }

        private void SetMeasuringPoint(Vector3 point, bool hit)
        {
            if (!hit) return;

            point = CheckForModifiersPressed(point);

            FirstPersonMeasurementPoint newPoint = CreateNewPoint(point);

            if (measurementSegments.Count > 0)
            {
                Color32 objectColor = lineColors[measurementSegments.Count % lineColors.Length];
                FirstPersonMeasurementSegment prevSegment = measurementSegments[^1];

                prevSegment.CreateLine(newPoint, objectColor);

                totalDistanceInMeters += prevSegment.LineDistance;
                totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);

                FirstPersonMeasurementElement measurementElement = Instantiate(measurementElementPrefab, measurementParent);
                measurementElement.transform.SetSiblingIndex(measurementParent.childCount - 2);
                prevSegment.SetElement(measurementElement, measurementSegments.Count, removeElementCallback);
            }

            FirstPersonMeasurementSegment measurementSegment = new FirstPersonMeasurementSegment(newPoint, measurementSegments.Count);
            measurementSegments.Add(measurementSegment);
        }

        private Vector3 CheckForModifiersPressed(Vector3 position)
        {
            if (measurementSegments.Count == 0)
                return position;

            FirstPersonMeasurementSegment lastSegment = measurementSegments[^1];
            Vector3 lastPoint = lastSegment.pointB != null ? lastSegment.pointB.transform.position : lastSegment.pointA.transform.position;

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

        private FirstPersonMeasurementPoint CreateNewPoint(Vector3 point)
        {
            FirstPersonMeasurementPoint newPoint = Instantiate(pointObject, point, Quaternion.identity);
            return newPoint;
        }

        public void OnElementRemoved(FirstPersonMeasurementElement element)
        {
            int index = measurementSegments.FindIndex(s => s.measurementElement == element);
            if (index == -1) return;

            FirstPersonMeasurementSegment segment = measurementSegments[index];

            if (index + 1 < measurementSegments.Count)
            {
                measurementSegments[index + 1].pointA = segment.pointA;
            }

            Destroy(segment.pointB.gameObject);
            Destroy(segment.measurementElement.gameObject);

            measurementSegments.RemoveAt(index);

            for (int i = index; i < measurementSegments.Count; i++)
            {
                measurementSegments[i].Refresh(i, lineColors[(i + 1) % lineColors.Length]);
            }

            RecalculateTotalDistance();
        }

        private void RemoveMeasuringPoint(Vector3 point, bool hit)
        {
            if (!hit) return;

            for (int i = measurementSegments.Count - 1; i >= 0; i--)
            {
                FirstPersonMeasurementSegment segment = measurementSegments[i];

                if (segment.pointB == null) continue;

                if (IsPointClose(point, segment.pointB.transform.position, POINT_DELETE_DISTANCE))
                {
                    RemoveSegmentAt(i);
                    break;
                }

                if (i == 0 && IsPointClose(point, segment.pointA.transform.position, POINT_DELETE_DISTANCE))
                {
                    RemoveFirstSegment();
                    break;
                }
            }
        }

        private bool IsPointClose(Vector3 a, Vector3 b, float distance) => (a - b).sqrMagnitude <= distance * distance;

        private void RemoveSegmentAt(int index)
        {
            FirstPersonMeasurementSegment segment = measurementSegments[index];

            if (index + 1 < measurementSegments.Count)
                measurementSegments[index + 1].pointA = segment.pointA;

            Destroy(segment.pointB.gameObject);
            Destroy(segment.measurementElement.gameObject);

            measurementSegments.RemoveAt(index);

            for (int i = index; i < measurementSegments.Count; i++)
                measurementSegments[i].Refresh(i, lineColors[(i + 1) % lineColors.Length]);

            RecalculateTotalDistance();
        }

        private void RemoveFirstSegment()
        {
            if (measurementSegments.Count == 0) return;

            FirstPersonMeasurementSegment firstSegment = measurementSegments[0];

            Destroy(firstSegment.pointA.gameObject);
            Destroy(firstSegment.measurementElement.gameObject);

            measurementSegments.RemoveAt(0);

            for (int i = 0; i < measurementSegments.Count; i++)
            {
                if (i == 0)
                {
                    measurementSegments[0].Refresh(0, lineColors[(i + 1) % lineColors.Length]);
                    measurementSegments[0].pointA.DisableVisuals();
                }
                else
                {
                    measurementSegments[i].pointA = measurementSegments[i - 1].pointB;
                    measurementSegments[i].Refresh(i, lineColors[(i + 1) % lineColors.Length]);
                }
            }

            RecalculateTotalDistance();
        }

        private void RecalculateTotalDistance()
        {
            totalDistanceInMeters = 0;

            for (int i = 0; i < measurementSegments.Count; i++)
            {
                totalDistanceInMeters += measurementSegments[i].LineDistance;
            }

            totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);
        }

        public void ResetMeasurements()
        {
            for (int i = measurementSegments.Count - 1; i >= 0; i--)
            {
                FirstPersonMeasurementSegment segment = measurementSegments[i];

                Destroy(segment.pointA.gameObject);
                if (segment.pointB != null)
                {
                    Destroy(segment.pointB.gameObject);
                    Destroy(segment.measurementElement.gameObject);
                }
            }

            measurementSegments.Clear();

            totalDistanceInMeters = 0;
            totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);
        }

        private string ConvertToUnits(float valueInMeters)
        {
            string units = "m";

            if (valueInMeters >= 1000)
            {
                units = "km";
                valueInMeters /= 1000;
            }

            return "~" + valueInMeters.ToString("F2") + units;
        }

        public List<FirstPersonMeasurementSegment> GetMeasurementSegments() => measurementSegments;
    }
}
