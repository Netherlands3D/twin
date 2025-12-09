using TMPro;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurementPoint : MonoBehaviour
    {
        [SerializeField] private TextMeshPro pointLetterText;

        public void Init(string pointLetter)
        {
            UpdatePointerLetter(pointLetter);
        }

        public void UpdatePointerLetter(string pointLetter) => pointLetterText.text = pointLetter;
    }
}
