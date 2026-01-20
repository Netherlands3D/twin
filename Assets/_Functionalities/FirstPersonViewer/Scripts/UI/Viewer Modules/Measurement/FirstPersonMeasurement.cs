using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurement : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void DownloadFileImmediate(string gameObjectName, string callbackMethodName, string filename, byte[] data, int dataSize);

        private const float POINT_DELETE_DISTANCE = 1f;

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

        private void Start()
        {
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();
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

                if (heldTime <= maxClickDuration) SetMeasuringPoint();
            }

            if (Mouse.current.rightButton.wasPressedThisFrame) RemoveMeasuringPoint();
        }

        private void SetMeasuringPoint()
        {
            raycaster.GetWorldPointAsync(clickPosition, (point, hit) =>
            {
                if (hit)
                {
                    //When the Lock key is pressed use the previous y position. 
                    if (measuringYLockModifier.action.IsPressed() && pointList.Count > 0)
                        point.y = pointList[pointList.Count - 1].transform.position.y;

                    if (measuringXZLockModifier.action.IsPressed() && pointList.Count > 0)
                    {
                        point.x = pointList[pointList.Count - 1].transform.position.x;
                        point.z = pointList[pointList.Count - 1].transform.position.z;
                    }

                    FirstPersonMeasurementPoint newPoint = CreateNewPoint(point);

                    if (pointList.Count > 1)
                    {
                        //Get the last added item.
                        int index = pointList.Count - 1;
                        FirstPersonMeasurementPoint prevPoint = pointList[index - 1];
                        Color32 objectColor = lineColors[(index - 1) % lineColors.Length];

                        float dst = prevPoint.LineDistance; //First one returns 1!
                        totalDistanceInMeters += dst;
                        totalDistanceText.text = "Totale afstand: " + ConvertToUnits(totalDistanceInMeters);

                        CreateNewElement(index, objectColor, dst);

                        SetPointLine(newPoint, prevPoint, objectColor, dst);
                    }
                }
            }, FirstPersonViewerCamera.FPVCamera, measurementLayerMask);
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

        private void SetPointLine(FirstPersonMeasurementPoint newPoint, FirstPersonMeasurementPoint prevPoint, Color32 color, float dst)
        {
            newPoint.SetLine(newPoint.transform.position, prevPoint.transform.position);
            newPoint.SetLineColor(color);

            Vector3 center = (newPoint.transform.position + prevPoint.transform.position) * .5f;
            newPoint.SetText(center + Vector3.up * textHeightAboveLines, dst);
        }

        private void RemoveMeasuringPoint()
        {
            if (pointList.Count == 0) return;

            raycaster.GetWorldPointAsync(Pointer.current.position.ReadValue(), (point, hit) =>
            {
                if (hit)
                {
                    for (int i = pointList.Count - 1; i >= 0; i--)
                    {
                        if (Vector3.Distance(point, pointList[i].transform.position) < POINT_DELETE_DISTANCE)
                        {
                            RemovePoint(pointList[i]);
                            break;
                        }
                    }
                }
            }, FirstPersonViewerCamera.FPVCamera, measurementLayerMask);
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
            for (int i = 0; i < pointList.Count; i++)
            {
                pointList[i].UpdatePointerLetter(GetAlphabetLetter(i));

                if (i > 0)
                {
                    Vector3 start = pointList[i].transform.position;
                    Vector3 end = pointList[i - 1].transform.position;
                    pointList[i].SetLine(start, end);
                    pointList[i].SetLineColor(lineColors[(i - 1) % lineColors.Length]);

                    Vector3 center = (start + end) / 2;
                    float dst = pointList[i].LineDistance;
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
                measurementElements[i].SetTextColor(lineColors[i % lineColors.Length]);
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

            return "~" + valueInMeters.ToString("F2") + units;
        }

        public void ExportToCSV()
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HHmm");
            string filename = $"meting_export_{timestamp}.csv";

            string csv = "Punt 1; Punt 2; Afstand in meters; Afstand vanaf A\n";

            float totalDst = 0;
            measurementElements.ForEach(measurement =>
            {
                totalDst += measurement.GetMeasurementDistance();
                csv += measurement.GetCSVOutput() + $";{totalDst.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',')}\n";
            });

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(csv);

#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadFileImmediate(gameObject.name, "", filename, bytes, bytes.Length);
#elif UNITY_EDITOR
            string path = UnityEditor.EditorUtility.SaveFilePanel("Export CSV", "", filename, "csv");

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, bytes);
            }
#endif
        }
    }
}
