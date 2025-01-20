using System;
using Netherlands3D.Sun;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
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
            sunTime.useCurrentTimeChanged.AddListener(SaveUseCurrentTime);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnDataChanged.RemoveListener(OnProjectDataChanged);
            sunTime.timeOfDayChanged.RemoveListener(SaveCurrentDateTime);
            sunTime.useCurrentTimeChanged.RemoveListener(SaveUseCurrentTime);
        }

        private void Start()
        {
            OnProjectDataChanged(ProjectData.Current); //set initial value based on the current project
        }
        
        private void OnProjectDataChanged(ProjectData project)
        {
            sunTime.UseCurrentTime = project.UseCurrentTime;
            
            if (!project.UseCurrentTime)
            {
                lastSavedDateTime = project.CurrentDateTime;
                sunTime.SetTime(project.CurrentDateTime);
            }
        }

        private void SaveCurrentDateTime(DateTime newDateTime)
        {
            if (lastSavedDateTime == newDateTime)
                return;

            lastSavedDateTime = newDateTime;
            ProjectData.Current.CurrentDateTime = newDateTime;
        }
        
        private void SaveUseCurrentTime(bool useCurrentTime)
        {
            ProjectData.Current.UseCurrentTime = useCurrentTime;
        }        
    }
}