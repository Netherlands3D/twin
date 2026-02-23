using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    public class ProjectDataCameraUpdater : MonoBehaviour
    {
        private Vector3 lastPosition;
        private Vector3 lastRotation;

        private void OnEnable()
        {
            ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnDataChanged.RemoveListener(OnProjectDataChanged);
        }
        
        private void OnProjectDataChanged(ProjectData project)
        {
            var cameraCoordinate = new Coordinate(CoordinateSystem.RDNAP, project.CameraPosition[0], project.CameraPosition[1], project.CameraPosition[2]);
            GetComponent<WorldTransform>().MoveToCoordinate(cameraCoordinate);

            if (project.CameraRotation.Length == 3)
            {
                var cameraRotation = new Vector3((float)project.CameraRotation[0], (float)project.CameraRotation[1], (float)project.CameraRotation[2]);
                transform.rotation = Quaternion.Euler(cameraRotation);
            }
        }

        private void Update() 
        {
            if (transform.position == lastPosition && transform.eulerAngles == lastRotation) return;

            lastPosition = transform.position;
            lastRotation = transform.eulerAngles;
            SaveCurrentCameraTransform();
        }

        private double[] camRDPosition = new double[3];
        private double[] camRotation = new double[3];
        private void SaveCurrentCameraTransform()
        {
            var cameraCoordinate = new Coordinate(transform.position).Convert(CoordinateSystem.RDNAP);
            var cameraCoordinateRD = new Vector3Double(cameraCoordinate.easting, cameraCoordinate.northing, cameraCoordinate.height);
            var cameraRotation = transform.eulerAngles;

            camRDPosition[0] = cameraCoordinateRD.x;
            camRDPosition[1] = cameraCoordinateRD.y;
            camRDPosition[2] = cameraCoordinateRD.z;
            ProjectData.Current.CameraPosition = camRDPosition;

            camRotation[0] = cameraRotation.x;
            camRotation[1] = cameraRotation.y;
            camRotation[2] = cameraRotation.z;
            ProjectData.Current.CameraRotation = camRotation;
        }
    }
}
