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

        public void Init(string pointLetter)
        {
            UpdatePointerLetter(pointLetter);
        }

        public void UpdatePointerLetter(string pointLetter) => pointLetterText.text = pointLetter;
        
        public void SetLine(Vector3 start, Vector3 end)
        {
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.gameObject.SetActive(true);
        }
        
        public void SetLine(Vector3 start, Vector3 end, Color32 color)
        {
            SetLine(start, end);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}
