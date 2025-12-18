using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurement : MonoBehaviour
    {
        [Header("Measuring")]
        [SerializeField] private InputActionReference mouseClick;
        [SerializeField] private LayerMask measurementLayerMask;
        private OpticalRaycaster raycaster;
        [SerializeField] private InputActionReference measuringHeightModifier;
        [SerializeField] private float maxClickDuration = 0.1f;
        private float clickDownTime;
        private Vector2 clickPosition;
        private int clickCount;

        [SerializeField] private FirstPersonMeasurementPoint pointObject;
        private List<FirstPersonMeasurementPoint> pointList;
        private float textHeightAboveLines = .65f;

        [Header("UI")]
        [SerializeField] private FirstPersonMeasurementElement measurementElementPrefab;
        [SerializeField] private Transform measurementParent;
        private List<FirstPersonMeasurementElement> measurementElements;

        [SerializeField] private Color32[] lineColors;
        [SerializeField] private TextMeshProUGUI totalDistanceText;
        private float totalDistanceInMeters;

        private void Start()
        {
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();
            pointList = new List<FirstPersonMeasurementPoint>();
            measurementElements = new List<FirstPersonMeasurementElement>();
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

                if (heldTime > maxClickDuration) return;

                raycaster.GetWorldPointAsync(clickPosition, (point, hit) =>
                {
                    if (hit)
                    {
                        if (measuringHeightModifier.action.IsPressed() && pointList.Count > 0)
                            point.y = pointList[pointList.Count - 1].transform.position.y;

                        FirstPersonMeasurementPoint newPoint = Instantiate(pointObject, point, Quaternion.identity);
                        newPoint.Init(GetAlphabetLetter(pointList.Count));
                        pointList.Add(newPoint);
                        clickCount++;

                        if (pointList.Count > 1)
                        {
                            FirstPersonMeasurementElement measurementElement = Instantiate(measurementElementPrefab, measurementParent);
                            measurementElement.transform.SetSiblingIndex(measurementParent.childCount - 2);

                            int index = pointList.Count - 1; //Hmm IDK about this yet.
                            FirstPersonMeasurementPoint prevPoint = pointList[index - 1];

                            float dst = Vector3.Distance(prevPoint.transform.position, newPoint.transform.position);

                            totalDistanceInMeters += dst;
                            totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);

                            Color32 objectColor = lineColors[clickCount % lineColors.Length];
                            measurementElement.Init(GetAlphabetLetter(index - 1), GetAlphabetLetter(index), dst, objectColor, RemoveElement);
                            measurementElements.Add(measurementElement);

                            newPoint.SetLine(newPoint.transform.position, prevPoint.transform.position);
                            newPoint.SetLineColor(objectColor);

                            Vector3 center = (newPoint.transform.position + prevPoint.transform.position) / 2;
                            newPoint.SetText(center + Vector3.up * textHeightAboveLines, dst);
                            newPoint.SetTextColor(objectColor);
                        }
                    }
                }, FirstPersonViewerCamera.FPVCamera, measurementLayerMask);
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (pointList == null) return;
                if (pointList.Count == 0) return;

                raycaster.GetWorldPointAsync(Pointer.current.position.ReadValue(), (point, hit) =>
                {
                    if (hit)
                    {
                        for (int i = pointList.Count - 1; i >= 0; i--)
                        {
                            if (Vector3.Distance(point, pointList[i].transform.position) < 1f)
                            {
                                RemovePoint(pointList[i]);
                                break;
                            }
                        }
                    }
                }, FirstPersonViewerCamera.FPVCamera, measurementLayerMask);
            }
        }

        public void ResetMeasurements()
        {
            if (pointList == null) return;

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
            clickCount = 0;
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
            for (int i = 0; i < pointList.Count; i++)
            {
                pointList[i].UpdatePointerLetter(GetAlphabetLetter(i));

                if (i > 0)
                {
                    Vector3 start = pointList[i].transform.position;
                    Vector3 end = pointList[i - 1].transform.position;
                    pointList[i].SetLine(start, end);

                    Vector3 center = (start + end) / 2;
                    float dst = Vector3.Distance(start, end);
                    pointList[i].SetText(center + Vector3.up * textHeightAboveLines, dst);
                }
                else if (i == 0) pointList[i].DisableVisuals();
            }

            float newTotalDistance = 0;
            for (int i = 0; i < measurementElements.Count; i++)
            {
                if (i + 1 >= pointList.Count)
                {
                    measurementElements[i].UpdateMeasurement(GetAlphabetLetter(i), "-", -1);
                    continue;
                }

                Vector3 pos1 = pointList[i].transform.position;
                Vector3 pos2 = pointList[i + 1].transform.position;

                float dst = Vector3.Distance(pos1, pos2);

                newTotalDistance += dst;
                measurementElements[i].UpdateMeasurement(GetAlphabetLetter(i), GetAlphabetLetter(i + 1), dst);
            }

            totalDistanceInMeters = newTotalDistance;
            totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);
        }

        private string GetAlphabetLetter(int index)
        {
            const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
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

            float roundedValue = Mathf.Round(valueInMeters * 100) / 100;
            return "~" + roundedValue + units;
        }
    }
}
