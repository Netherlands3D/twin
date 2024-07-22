using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ProjectDataCameraUpdater : MonoBehaviour
    {
        // [SerializeField] private ProjectData project;

        private Matrix4x4 lastSavedCameraTransformMatrix = Matrix4x4.identity;

        private void Update() {
            var currentCameraMatrix = transform.localToWorldMatrix;        
            if(Matrix4x4.Equals(currentCameraMatrix, lastSavedCameraTransformMatrix)) return;

            lastSavedCameraTransformMatrix = currentCameraMatrix;
            SaveCurrentCameraTransform();
        }

        private void SaveCurrentCameraTransform()
        {
            var cameraCoordinate = CoordinateConverter.ConvertTo(new Coordinate(CoordinateSystem.Unity,transform.position[0], transform.position[1],transform.position[2]), CoordinateSystem.RD);
            var cameraRotation = transform.eulerAngles;

            ProjectData.Current.CameraPosition = new double[] { cameraCoordinate.Points[0], cameraCoordinate.Points[1], cameraCoordinate.Points[2] };
            ProjectData.Current.CameraRotation = new double[] { cameraRotation.x, cameraRotation.y, cameraRotation.z };
        }

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
            var cameraCoordinate = new Coordinate(CoordinateSystem.RD, project.CameraPosition[0], project.CameraPosition[1], project.CameraPosition[2]);
            var cameraUnityCoordinate = CoordinateConverter.ConvertTo(cameraCoordinate, CoordinateSystem.Unity).ToVector3();

            transform.position = cameraUnityCoordinate;

            if(project.CameraRotation.Length == 3)
            {
                var cameraRotation = new Vector3((float)project.CameraRotation[0], (float)project.CameraRotation[1], (float)project.CameraRotation[2]);
                transform.rotation = Quaternion.Euler(cameraRotation);
            }
        }
    }
}
