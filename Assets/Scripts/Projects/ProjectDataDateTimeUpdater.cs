using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(SunTime))]
    public class ProjectDataDateTimeUpdater : MonoBehaviour
    {
        private DateTime lastSavedDateTime = new();
        private SunTime sunTime;

        private void Awake()
        {
            sunTime = GetComponent<SunTime>();
        }

        private void OnEnable()
        {
            ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);
            sunTime.timeOfDayChanged.AddListener(SaveCurrentDateTime);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnDataChanged.RemoveListener(OnProjectDataChanged);
            sunTime.timeOfDayChanged.RemoveListener(SaveCurrentDateTime);
        }

        private void Start()
        {
            OnProjectDataChanged(ProjectData.Current); //set initial value based on the current project
        }

        private void OnProjectDataChanged(ProjectData project)
        {
            lastSavedDateTime = project.CurrentDateTime;
            sunTime.SetTime(project.CurrentDateTime);
        }
        
        private void SaveCurrentDateTime(DateTime newDateTime)
        {
            if(lastSavedDateTime == newDateTime)
                return;
            
            lastSavedDateTime = newDateTime;
            ProjectData.Current.CurrentDateTime = newDateTime;
        }
    }
}
