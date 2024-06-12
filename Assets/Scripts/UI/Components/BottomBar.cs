using System.Resources;
using Netherlands3D.Coordinates;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class BottomBar : MonoBehaviour
    {
        [Header("Camera coordinates")]
        [SerializeField] private TextMeshProUGUI coordinatesText;
        [SerializeField] private string coordinateFormat = "x{0} y{1} z{2}";
        public void Update()
        {
            ApplyCameraPositionToText();
        }

        private void ApplyCameraPositionToText()
        {
            //Use coordinate convert to convert camera to rd coordinates
            var cameraCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                Camera.main.transform.position.x,
                Camera.main.transform.position.y,
                Camera.main.transform.position.z
             );
            var rd = CoordinateConverter.ConvertTo(cameraCoordinate, CoordinateSystem.RDNAP);

            //Replace the placeholders with the coordinates
            coordinatesText.text = coordinateFormat
                .Replace("{0}", rd.Points[0].ToString("0"))
                .Replace("{1}", rd.Points[1].ToString("0"))
                .Replace("{2}", rd.Points[2].ToString("0"));
        }
    }
}
