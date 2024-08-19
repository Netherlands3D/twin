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
            print("updating time from project: " + project.CurrentDateTime);
            lastSavedDateTime = project.CurrentDateTime;
            sunTime.SetTime(project.CurrentDateTime);
        }
        
        private void SaveCurrentDateTime(DateTime newDateTime)
        {
            print("try saving time to project: " + newDateTime + "\tLastSavedTime: " + lastSavedDateTime);
            if(lastSavedDateTime == newDateTime)
                return;
            
            lastSavedDateTime = newDateTime;
            
            print("new time is different, saving to project: " + newDateTime);
            ProjectData.Current.CurrentDateTime = newDateTime;
        }
    }
}
