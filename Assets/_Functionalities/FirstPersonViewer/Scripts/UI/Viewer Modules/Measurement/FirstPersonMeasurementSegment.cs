using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.tvOS;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurementSegment
    {
        private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const float TEXT_HEIGHT_ABOVE_LINE = .65f;

        public FirstPersonMeasurementPoint pointA;
        public FirstPersonMeasurementPoint pointB;

        public FirstPersonMeasurementElement measurementElement;

        public float LineDistance { private set; get; }
        private Color32 lineColor;

        public FirstPersonMeasurementSegment(FirstPersonMeasurementPoint start, int index)
        {
            pointA = start;
            pointA.Init(GetAlphabetLetter(index));
        }

        public void CreateLine(FirstPersonMeasurementPoint end, Color32 color)
        {
            pointB = end;
            lineColor = color;

            pointB.SetLine(pointB.transform.position, pointA.transform.position);
            pointB.SetLineColor(color);

            LineDistance = pointB.LineDistance;

            Vector3 center = (pointB.transform.position + pointA.transform.position) * .5f;
            pointB.SetText(center + Vector3.up * TEXT_HEIGHT_ABOVE_LINE, LineDistance);
        }

        public void SetElement(FirstPersonMeasurementElement measurementElement, int index, Action<FirstPersonMeasurementElement> OnPointRemovedCallback)
        {
            this.measurementElement = measurementElement;

            measurementElement.Init(GetAlphabetLetter(index - 1), GetAlphabetLetter(index), LineDistance, lineColor, OnPointRemovedCallback);
        }

        public void Refresh(int index, Color color)
        {
            string pointAText = GetAlphabetLetter(index);
            string pointBText = GetAlphabetLetter(index + 1);

            pointA.UpdatePointerLetter(pointAText);

            //The last element always has PointB as null (because a new point connects it).
            if (pointB != null)
            {
                pointB.UpdatePointerLetter(pointBText);
                pointB.SetLine(pointB.transform.position, pointA.transform.position);
                pointB.SetLineColor(color);

                LineDistance = pointB.LineDistance;

                Vector3 center = (pointB.transform.position + pointA.transform.position) * .5f;
                pointB.SetText(center + Vector3.up * TEXT_HEIGHT_ABOVE_LINE, LineDistance);

                measurementElement.UpdateMeasurement(pointAText, pointBText, LineDistance);
                measurementElement.SetTextColor(color);
            }
        }

        public void RemovePoint(bool removeB)
        {
            if (removeB) GameObject.Destroy(pointB.gameObject);
            else GameObject.Destroy(pointA.gameObject);
            GameObject.Destroy(measurementElement.gameObject);
        }

        public void Dispose()
        {
            GameObject.Destroy(pointA.gameObject);
            if (pointB != null)
            {
                GameObject.Destroy(pointB.gameObject);
                GameObject.Destroy(measurementElement.gameObject);
            }
        }


        public string GetCSVOutput() => $"{pointA.GetLetter()}; {pointB.GetLetter()}; {LineDistance.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',')}";

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
    }
}
