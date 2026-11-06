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
        // SunTime stores speed as seconds/second internally.
        // The UI shows speed in hours/second so we apply this factor.
        private const float SecondsPerHour = 3600f;
        
        private void Awake()
        {
            sunTimePanel = App.UIRoot.Root.Q<SunTimePanel>();
        }

        private void Start()
        {
            timeService = Services.ServiceLocator.GetService<TimeService>();
            periodicSnapshotsService = Services.ServiceLocator.GetService<PeriodicSnapshots>();
            sunTimePanel.OnTimeOfDayChanged(timeService.Time); 
            sunTimePanel.SetMoments(periodicSnapshotsService.Moments);
            sunTimePanel.DownloadButton.clicked += periodicSnapshotsService.DownloadSnapshots;
            sunTimePanel.NowButton.clicked += timeService.ResetToNow;
            sunTimePanel.SunDial.TimeChanged += (h,m) => timeService.SetTime(h, m, 0);
            sunTimePanel.SimulationSpeedControls.SpeedChanged += (hps) => timeService?.SetTimeSpeed(hps * SecondsPerHour);;
            sunTimePanel.SimulationSpeedControls.PlayToggled += timeService.ToggleAnimation;
            sunTimePanel.DateField.SubmitEvent += OnDateChanged;
            sunTimePanel.TimeChanged.AddListener(OnTimeChanged);
            
            periodicSnapshotsService.onStartGenerating.AddListener(CacheTime);
            periodicSnapshotsService.SetTimeForSnapshot.AddListener(SetTimeForSnapshot);
            periodicSnapshotsService.onFinishedGenerating.AddListener(ResetTimeAfterSnapshots);
            
            timeService.timeOfDayChanged.AddListener(sunTimePanel.OnTimeOfDayChanged);
            timeService.timeSpeedChanged.AddListener(OnTimeSpeedChanged);
        }
       
        private void OnDestroy()
        {
            periodicSnapshotsService.onStartGenerating.RemoveListener(CacheTime);
            periodicSnapshotsService.SetTimeForSnapshot.RemoveListener(SetTimeForSnapshot);
            periodicSnapshotsService.onFinishedGenerating.RemoveListener(ResetTimeAfterSnapshots);
            
            timeService.timeOfDayChanged.RemoveListener(sunTimePanel.OnTimeOfDayChanged);
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
        
        public void OnTimeSpeedChanged(float speedSecondsPerSecond)
        {
            sunTimePanel.SimulationSpeedControls.SetSpeedWithoutNotify(speedSecondsPerSecond / SecondsPerHour);
        }
        
        private void OnDateChanged(int day, int month, int year)
        {
            timeService?.SetDay(day);
            timeService?.SetMonth(month);
            timeService?.SetYear(year);
        }
        
        private void OnTimeChanged()
        {
            var dt = sunTimePanel.TimeField.GetValueAsTime(timeService.Time);
            timeService?.SetTime(dt.Hour, dt.Minute, 0);
        }
    }
}
