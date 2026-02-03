using System;
using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurementSegment : MonoBehaviour
    {
        private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const float TEXT_HEIGHT_ABOVE_LINE = .65f;
        
        public FirstPersonMeasurementPoint PointA => pointA;
        public FirstPersonMeasurementPoint PointB => pointB;
        public FirstPersonMeasurementElement Element => measurementElement;

        private FirstPersonMeasurementPoint pointA;
        private FirstPersonMeasurementPoint pointB;

        private FirstPersonMeasurementElement measurementElement;
        private Color lineColor;
        [Header("Line")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private TextMeshPro lineDistanceText;
        
        
        public float LineDistance => PointB == null || PointA == null ? 0 : (pointA.Postion - pointB.Postion).magnitude;

        void Start()
        {
            lineDistanceText.isOverlay = true;
        }
        
        public void SetFirstPoint(FirstPersonMeasurementPoint start, int index)
        {
            pointA = start;
            pointA.Init(GetAlphabetLetter(index));
        }

        public void SetSecondPoint(FirstPersonMeasurementPoint end, Color color)
        {
            pointB = end;
            lineColor = color;

            SetLine();
            SetLineColor(color);
            
            if(pointA == null || pointB == null) return;
            
            Vector3 center = (pointB.transform.position + pointA.transform.position) * .5f;
            SetText(center + Vector3.up * TEXT_HEIGHT_ABOVE_LINE, LineDistance);
        }

        public void SetElement(FirstPersonMeasurementElement measurementElement, int index, Action<FirstPersonMeasurementElement> OnPointRemovedCallback)
        {
            this.measurementElement = measurementElement;
            measurementElement.Init(GetAlphabetLetter(index - 1), GetAlphabetLetter(index), LineDistance, lineColor, OnPointRemovedCallback);
        }
        
        public void SetLine()
        {
            if (pointB == null)
            {
                DisableVisuals();
                return;
            }
            
            lineRenderer.SetPosition(0, pointB.Postion);
            lineRenderer.SetPosition(1, pointA.Postion);
            lineRenderer.gameObject.SetActive(true);
        }
        
        public void SetLineColor(Color color)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
        
        public void SetText(Vector3 center, float distance)
        {
            lineDistanceText.text =  $"~{distance.ToString("F2")}m";
            lineDistanceText.transform.position = center;
            lineDistanceText.gameObject.SetActive(true);
        }

        public void DisableVisuals()
        {
            lineRenderer.gameObject.SetActive(false);
            lineDistanceText.gameObject.SetActive(false);
        }

        public void Refresh(int index, Color color)
        {
            string pointAText = GetAlphabetLetter(index);
            string pointBText = GetAlphabetLetter(index + 1);

            pointA.UpdatePointerLetter(pointAText);

            measurementElement?.gameObject.SetActive(pointB != null);
            //The last element always has PointB as null (because a new point connects it).
            if (pointB != null)
            {
                pointB.UpdatePointerLetter(pointBText);
                SetLine();
                SetLineColor(color);

                Vector3 center = (pointB.transform.position + pointA.transform.position) * .5f;
                SetText(center + Vector3.up * TEXT_HEIGHT_ABOVE_LINE, LineDistance);

                if(measurementElement == null) return;
                
                measurementElement.UpdateMeasurement(pointAText, pointBText, LineDistance);
                measurementElement.SetTextColor(color);
            }
            
        }

        public Vector3 GetLastPosition()
        {
            return pointB != null ? pointB.transform.position : pointA.transform.position;
        }

        public void Remove()
        {
            if(PointB != null) Destroy(pointB.gameObject);
            Destroy(pointA.gameObject);
            if(measurementElement != null) Destroy(measurementElement.gameObject);
        }

        public void RemoveFirstPoint()
        {
            Destroy(pointA.gameObject);
            if(measurementElement != null) Destroy(measurementElement.gameObject);
        }

        private string GetAlphabetLetter(int index)
        {
            int baseVal = ALPHABET.Length;
            string result = "";
            while (index >= 0)
            {
                result = ALPHABET[index % baseVal] + result;
                index /= baseVal;
                index--;
            }
            return result;
        }
    }
}
