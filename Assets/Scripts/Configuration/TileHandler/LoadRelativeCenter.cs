using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration.TileHandler
{
    public class LoadRelativeCenter : MonoBehaviour
    {
        [SerializeField] private Configurator configurator;

        private void OnEnable()
        {
            configurator.Configuration.OnOriginChanged.AddListener(Apply);
        }

        private void OnDisable()
        {
            configurator.Configuration.OnOriginChanged.RemoveListener(Apply);
        }

        private void Apply(Coordinate coordinate)
        {
            // we have to move to camera to the required position,
            // moving the origin will be done by the Origin-script at the end of the frame;
            Camera cam = Camera.main;
            Coordinate CameraCoordinate = new Coordinate(cam.transform.position).Convert(CoordinateSystem.WGS84_LatLonHeight);
            double camElevation = CameraCoordinate.Points[2];

            Coordinate NewCamPositition = coordinate.Convert(CoordinateSystem.WGS84_LatLonHeight);
            NewCamPositition.Points[2] = camElevation;
            cam.transform.position = NewCamPositition.ToUnity();
       
        }
    }
}