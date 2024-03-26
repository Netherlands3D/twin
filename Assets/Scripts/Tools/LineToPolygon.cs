using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class LineToPolygon : MonoBehaviour
    {
        private List<Vector3> originalLine;
        private List<Vector3> polygon;
        public UnityEvent<List<Vector3>> OnPolygonCreated;
        public UnityEvent<List<Vector3>> OnPolygonEdited;

        private float currentWidth = 1.0f;

        public void SetLine(List<Vector3> line)
        {
            originalLine = line;
            SetLineWidth(currentWidth);
        }

        public void UpdateLine(List<Vector3> line)
        {
            SetLine(line);
            OnPolygonEdited.Invoke(polygon);
        }

        public void SetLineWidth(float width)
        {
            if (originalLine == null)
            {
                Debug.LogError("No line set");
                return;
            }

            polygon = new List<Vector3>();
            for (int i = 0; i < originalLine.Count; i++)
            {
                var startPoint = originalLine[i];
                var endPoint = originalLine[(i + 1) % originalLine.Count];

                var direction1 = new Vector3(endPoint.y - startPoint.y, startPoint.x - endPoint.x, 0).normalized * width;
                var direction2 = new Vector3(startPoint.y - endPoint.y, endPoint.x - startPoint.x, 0).normalized * width;

                var p1 = startPoint + direction1;
                var p2 = endPoint + direction1;
                var p3 = endPoint + direction2;
                var p4 = startPoint + direction2;

                polygon.Add(p1);
                polygon.Add(p2);
                polygon.Add(p3);
                polygon.Add(p4);
            }

            OnPolygonCreated.Invoke(polygon);
        }
    }
}
