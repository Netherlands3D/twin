using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurementPoint : MonoBehaviour
    {
        [SerializeField] private TextMeshPro pointLetterText;
        public Vector3 Postion => transform.position;

        public void Init(string pointLetter)
        {
            UpdatePointerLetter(pointLetter);
            pointLetterText.isOverlay = true;
        }

        public void UpdatePointerLetter(string pointLetter) => pointLetterText.text = pointLetter;
        public string GetLetter() => pointLetterText.text;
    }
}
