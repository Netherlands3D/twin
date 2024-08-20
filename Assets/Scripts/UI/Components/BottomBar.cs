using System.Resources;
using Netherlands3D.Coordinates;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class BottomBar : MonoBehaviour
    {
        private Camera mainCamera;

        [Header("Camera coordinates")]
        [SerializeField] private TextMeshProUGUI coordinatesText;
        [SerializeField] private string coordinateFormat = "x{0} y{1} z{2}";

        private void Start()
        {
            mainCamera = Camera.main;
        }

        public void Update()
        {
            ApplyCameraPositionToText();
        }

        private void ApplyCameraPositionToText()
        {
            var cameraCoordinate = new Coordinate(mainCamera.transform.position);
            var rd = cameraCoordinate.Convert(CoordinateSystem.RDNAP);

            //Replace the placeholders with the coordinates
            coordinatesText.text = coordinateFormat
                .Replace("{0}", rd.Points[0].ToString("0"))
                .Replace("{1}", rd.Points[1].ToString("0"))
                .Replace("{2}", rd.Points[2].ToString("0"));
        }
    }
}
