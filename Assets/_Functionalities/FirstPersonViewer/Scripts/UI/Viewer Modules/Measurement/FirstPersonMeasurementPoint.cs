using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurementPoint : MonoBehaviour
    {
        [SerializeField] private TextMeshPro pointLetterText;

        [Header("Line")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private TextMeshPro lineDistanceText;

        public Vector3 Postion => transform.position;

        public void Init(string pointLetter)
        {
            UpdatePointerLetter(pointLetter);
            lineDistanceText.isOverlay = true;
            pointLetterText.isOverlay = true;
        }

        public void UpdatePointerLetter(string pointLetter) => pointLetterText.text = pointLetter;
        
        public void SetLine(Vector3 start, Vector3 end)
        {
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
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

        public void SetTextColor(Color color) => lineDistanceText.color = color;

        public void DisableVisuals()
        {
            lineRenderer.gameObject.SetActive(false);
            lineDistanceText.gameObject.SetActive(false);
        }

        public string GetLetter() => pointLetterText.text;
    }
}
