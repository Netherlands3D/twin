using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    public class ProjectDataCameraUpdater : MonoBehaviour
    {
        private Matrix4x4 lastSavedCameraTransformMatrix = Matrix4x4.identity;

        private void OnEnable()
        {
            ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnDataChanged.RemoveListener(OnProjectDataChanged);
        }

        private void Start()
        {
            OnProjectDataChanged(ProjectData.Current); //set initial position based on the current project
        }

        private void OnProjectDataChanged(ProjectData project)
        {
            var cameraCoordinate = new Coordinate(CoordinateSystem.RDNAP, project.CameraPosition[0], project.CameraPosition[1], project.CameraPosition[2]);
            var cameraUnityCoordinate = cameraCoordinate.ToUnity();
            transform.position = cameraUnityCoordinate;

            if(project.CameraRotation.Length == 3)
            {
                var cameraRotation = new Vector3((float)project.CameraRotation[0], (float)project.CameraRotation[1], (float)project.CameraRotation[2]);
                transform.rotation = Quaternion.Euler(cameraRotation);
            }
        }

        private void Update() {
            var currentCameraMatrix = transform.localToWorldMatrix;        
            if(Matrix4x4.Equals(currentCameraMatrix, lastSavedCameraTransformMatrix)) return;

            lastSavedCameraTransformMatrix = currentCameraMatrix;
            SaveCurrentCameraTransform();
        }

        private void SaveCurrentCameraTransform()
        {
            var cameraCoordinate = new Coordinate(CoordinateSystem.Unity, transform.position.x, transform.position.y, transform.position.z);
            var cameraCoordinateRD = cameraCoordinate.Convert(CoordinateSystem.RDNAP).ToVector3RD();
            var cameraRotation = transform.eulerAngles;

            ProjectData.Current.CameraPosition = new double[] { cameraCoordinateRD.x, cameraCoordinateRD.y, cameraCoordinateRD.z };
            ProjectData.Current.CameraRotation = new double[] { cameraRotation.x, cameraRotation.y, cameraRotation.z };
        }
    }
}
