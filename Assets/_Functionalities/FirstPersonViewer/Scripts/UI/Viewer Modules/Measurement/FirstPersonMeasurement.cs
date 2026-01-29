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
        private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";


        private OpticalRaycaster raycaster;

        [Header("Measuring")]
        [SerializeField] private InputActionReference mouseClick;
        [SerializeField] private InputActionReference measuringYLockModifier;
        [SerializeField] private InputActionReference measuringXZLockModifier;
        [SerializeField] private LayerMask measurementLayerMask;
        [SerializeField] private float maxClickDuration = 0.1f;
        private float clickDownTime;
        private Vector2 clickPosition;

        [SerializeField] private FirstPersonMeasurementPoint pointObject;
        private List<FirstPersonMeasurementPoint> pointList = new List<FirstPersonMeasurementPoint>();
        [SerializeField] private float textHeightAboveLines = .65f;

        [Header("UI")]
        [SerializeField] private FirstPersonMeasurementElement measurementElementPrefab;
        [SerializeField] private Transform measurementParent;
        private List<FirstPersonMeasurementElement> measurementElements = new List<FirstPersonMeasurementElement>();

        [SerializeField] private Color32[] lineColors;
        [SerializeField] private TextMeshProUGUI totalDistanceText;
        private float totalDistanceInMeters;

        private Action<Vector3, bool> setMeasurementPointCallback;
        private Action<Vector3, bool> removeMeasurementPointCallback;

        private void Start()
        {
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            setMeasurementPointCallback = SetMeasuringPoint;
            removeMeasurementPointCallback = RemoveMeasuringPoint;
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

            if (Mouse.current.rightButton.wasPressedThisFrame && pointList.Count > 0)
                raycaster.GetWorldPointAsync(Pointer.current.position.ReadValue(), removeMeasurementPointCallback, App.Cameras.ActiveCamera, measurementLayerMask);
        }

        private void SetMeasuringPoint(Vector3 point, bool hit)
        {
            if (!hit) return;

            point = CheckForModifiersPressed(point);

            FirstPersonMeasurementPoint newPoint = CreateNewPoint(point);

            if (pointList.Count > 1)
            {
                //Get the last added item.
                int index = pointList.Count - 1;
                FirstPersonMeasurementPoint prevPoint = pointList[index - 1];
                Color32 objectColor = lineColors[(index - 1) % lineColors.Length];

                SetPointLine(newPoint, prevPoint, objectColor);

                float dst = newPoint.LineDistance;
                totalDistanceInMeters += dst;
                totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);

                //Text center on line.
                Vector3 center = (newPoint.transform.position + prevPoint.transform.position) * .5f;
                newPoint.SetText(center + Vector3.up * textHeightAboveLines, dst);

                CreateNewElement(index, objectColor, dst);
            }
        }

        private Vector3 CheckForModifiersPressed(Vector3 position)
        {
            //When the Lock key is pressed use the previous y position. 
            if (measuringYLockModifier.action.IsPressed() && pointList.Count > 0)
                position.y = pointList[pointList.Count - 1].transform.position.y;

            if (measuringXZLockModifier.action.IsPressed() && pointList.Count > 0)
            {
                position.x = pointList[pointList.Count - 1].transform.position.x;
                position.z = pointList[pointList.Count - 1].transform.position.z;
            }

            return position;
        }

        private FirstPersonMeasurementPoint CreateNewPoint(Vector3 point)
        {
            FirstPersonMeasurementPoint newPoint = Instantiate(pointObject, point, Quaternion.identity);
            newPoint.Init(GetAlphabetLetter(pointList.Count));
            pointList.Add(newPoint);
            return newPoint;
        }

        private void CreateNewElement(int index, Color32 color, float dst)
        {
            FirstPersonMeasurementElement measurementElement = Instantiate(measurementElementPrefab, measurementParent);
            measurementElement.transform.SetSiblingIndex(measurementParent.childCount - 2);

            measurementElement.Init(GetAlphabetLetter(index - 1), GetAlphabetLetter(index), dst, color, RemoveElement);
            measurementElements.Add(measurementElement);
        }

        private void SetPointLine(FirstPersonMeasurementPoint newPoint, FirstPersonMeasurementPoint prevPoint, Color32 color)
        {
            newPoint.SetLine(newPoint.transform.position, prevPoint.transform.position);
            newPoint.SetLineColor(color);
        }

        private void RemoveMeasuringPoint(Vector3 point, bool hit)
        {
            if (!hit) return;

            for (int i = pointList.Count - 1; i >= 0; i--)
            {
                float sqrDist = (point - pointList[i].transform.position).sqrMagnitude;
                if (sqrDist < POINT_DELETE_DISTANCE * POINT_DELETE_DISTANCE)
                {
                    RemovePoint(pointList[i]);
                    break;
                }
            }
        }

        public void ResetMeasurements()
        {
            for (int i = pointList.Count - 1; i >= 0; i--)
            {
                Destroy(pointList[i].gameObject);
            }

            pointList.Clear();

            for (int i = measurementElements.Count - 1; i >= 0; i--)
            {
                Destroy(measurementElements[i].gameObject);
            }

            measurementElements.Clear();

            totalDistanceInMeters = 0;
            totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);
        }

        private void RemoveElement(FirstPersonMeasurementElement element)
        {
            int index = measurementElements.IndexOf(element);

            Destroy(pointList[index + 1].gameObject);
            pointList.RemoveAt(index + 1);

            measurementElements.Remove(element);
            Destroy(element.gameObject);

            RefreshMeasurements();
        }

        private void RemovePoint(FirstPersonMeasurementPoint point)
        {
            int index = pointList.IndexOf(point);

            //We can't remove point A withouth breaking everything
            if (index == 0)
            {
                Destroy(pointList[index].gameObject);
                pointList.RemoveAt(index);

                if (measurementElements.Count > 0)
                {
                    Destroy(measurementElements[0].gameObject);
                    measurementElements.RemoveAt(0);
                }

                RefreshMeasurements();
            }
            else RemoveElement(measurementElements[index - 1]);
        }

        private void RefreshMeasurements()
        {
            for (int i = 0; i < pointList.Count - 1; i++)
            {
                if (i == 0) pointList[i].DisableVisuals();

                pointList[i].UpdatePointerLetter(GetAlphabetLetter(i));

                Vector3 start = pointList[i].transform.position;
                Vector3 end = pointList[i + 1].transform.position;

                pointList[i].SetLine(start, end);
                pointList[i].SetLineColor(lineColors[i % lineColors.Length]);

                Vector3 center = (start + end) * 0.5f;
                pointList[i].SetText(center + Vector3.up * textHeightAboveLines, pointList[i].LineDistance);
            }

            if (pointList.Count > 0) pointList[^1].UpdatePointerLetter(GetAlphabetLetter(pointList.Count - 1));


            float newTotalDistance = 0;
            for (int i = 0; i < measurementElements.Count; i++)
            {
                if (i + 1 >= pointList.Count)
                {
                    measurementElements[i].UpdateMeasurement(GetAlphabetLetter(i), "-", -1);
                    continue;
                }

                float dst = pointList[i + 1].LineDistance;

                newTotalDistance += dst;
                measurementElements[i].UpdateMeasurement(GetAlphabetLetter(i), GetAlphabetLetter(i + 1), dst);
                measurementElements[i].SetTextColor(lineColors[i % lineColors.Length]);
            }

            totalDistanceInMeters = newTotalDistance;
            totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);
        }

        private string GetAlphabetLetter(int index)
        {
            int baseVal = ALPHABET.Length;

            string result = "";

            index++;

            while (index > 0)
            {
                index--;
                result = ALPHABET[index % baseVal] + result;
                index /= baseVal;
            }

            return result;
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

        public List<FirstPersonMeasurementElement> GetMeasurementElements() => measurementElements;
    }
}
