using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ProjectDataCameraUpdater : MonoBehaviour
    {
        [SerializeField] private ProjectData project;

        private void OnEnable()
        {
            project.OnDataChanged.AddListener(OnProjectDataChanged);
        }

        private void OnDisable()
        {
            project.OnDataChanged.RemoveListener(OnProjectDataChanged);
        }

        private void OnProjectDataChanged(ProjectData project)
        {
            var cameraCoordinate = new Coordinate(CoordinateSystem.RD, project.CameraPosition[0], project.CameraPosition[1], project.CameraPosition[2]);
            var cameraUnityCoordinate = CoordinateConverter.ConvertTo(cameraCoordinate, CoordinateSystem.Unity).ToVector3();

            transform.position = cameraUnityCoordinate;

            //Euler if 3 values, 4 is quaternion
            if(project.CameraRotation.Length == 3)
            {
                var cameraRotation = new Vector3((float)project.CameraRotation[0], (float)project.CameraRotation[1], (float)project.CameraRotation[2]);
                transform.rotation = Quaternion.Euler(cameraRotation);
            }
            else if(project.CameraRotation.Length == 4)
            {
                var cameraRotation = new Quaternion((float)project.CameraRotation[0], (float)project.CameraRotation[1], (float)project.CameraRotation[2], (float)project.CameraRotation[3]);
                transform.rotation = cameraRotation;
            }
        }
    }
}
