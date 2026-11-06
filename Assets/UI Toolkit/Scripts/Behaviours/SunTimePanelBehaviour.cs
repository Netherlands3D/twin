using System;
using Netherlands3D.Credentials;
using Netherlands3D.ProjectTime;
using Netherlands3D.Snapshots;
using Netherlands3D.Twin;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    public class SunTimePanelBehaviour : MonoBehaviour
    {
        private SunTimePanel sunTimePanel;
        private TimeService timeService;
        private PeriodicSnapshots periodicSnapshotsService;
        private DateTime cachedTime;
        
        private void Awake()
        {
            sunTimePanel = App.UIRoot.Root.Q<SunTimePanel>();
        }

        private void Start()
        {
            timeService = Services.ServiceLocator.GetService<TimeService>();
            periodicSnapshotsService = Services.ServiceLocator.GetService<PeriodicSnapshots>();
        }

        private void OnEnable()
        {
            periodicSnapshotsService.onStartGenerating.AddListener(CacheTime);
            periodicSnapshotsService.SetTimeForSnapshot.AddListener(SetTimeForSnapshot);
            periodicSnapshotsService.onFinishedGenerating.AddListener(ResetTimeAfterSnapshots);
            
            timeService.timeOfDayChanged.AddListener(OnTimeOfDayChanged);
            timeService.timeSpeedChanged.AddListener(OnTimeSpeedChanged);
        }

        private void OnDisable()
        {
            periodicSnapshotsService.onStartGenerating.RemoveListener(CacheTime);
            periodicSnapshotsService.SetTimeForSnapshot.RemoveListener(SetTimeForSnapshot);
            periodicSnapshotsService.onFinishedGenerating.RemoveListener(ResetTimeAfterSnapshots);
            
            timeService.timeOfDayChanged.RemoveListener(OnTimeOfDayChanged);
            timeService.timeSpeedChanged.RemoveListener(OnTimeSpeedChanged);
        }

        private void CacheTime()
        {
            cachedTime = timeService.Time;
        }

        private void SetTimeForSnapshot(DateTime dt)
        {
            timeService.SetTime(dt);
        }

        private void ResetTimeAfterSnapshots()
        {
            timeService.SetTime(cachedTime);
        }
    }
}
