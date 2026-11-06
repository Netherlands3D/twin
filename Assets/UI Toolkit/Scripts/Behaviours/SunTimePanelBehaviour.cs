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
        private ToolService toolService;
        // SunTime stores speed as seconds/second internally.
        // The UI shows speed in hours/second so we apply this factor.
        private const float SecondsPerHour = 3600f;

        private void Start()
        {
            timeService = Services.ServiceLocator.GetService<TimeService>();
            periodicSnapshotsService = Services.ServiceLocator.GetService<PeriodicSnapshots>();
            toolService = Services.ServiceLocator.GetService<ToolService>();
            toolService.GetTool(ToolType.SunPosition).onOpen.AddListener(OnOpenPanel);
            toolService.GetTool(ToolType.SunPosition).onClose.AddListener(OnClosePanel);
            
            periodicSnapshotsService.onStartGenerating.AddListener(CacheTime);
            periodicSnapshotsService.SetTimeForSnapshot.AddListener(SetTimeForSnapshot);
            periodicSnapshotsService.onFinishedGenerating.AddListener(ResetTimeAfterSnapshots);
        }
       
        private void OnDestroy()
        {
            toolService.GetTool(ToolType.SunPosition).onOpen.RemoveListener(OnOpenPanel);
            toolService.GetTool(ToolType.SunPosition).onClose.RemoveListener(OnClosePanel);
            
            periodicSnapshotsService.onStartGenerating.RemoveListener(CacheTime);
            periodicSnapshotsService.SetTimeForSnapshot.RemoveListener(SetTimeForSnapshot);
            periodicSnapshotsService.onFinishedGenerating.RemoveListener(ResetTimeAfterSnapshots);
        }
       
        private void OnOpenPanel()
        {
            sunTimePanel = App.UIRoot.Root.Q<SunTimePanel>();
            sunTimePanel.OnTimeOfDayChanged(timeService.Time); 
            sunTimePanel.SetMoments(periodicSnapshotsService.Moments);
            sunTimePanel.DownloadButton.clicked += periodicSnapshotsService.DownloadSnapshots;
            sunTimePanel.NowButton.clicked += timeService.ResetToNow;
            sunTimePanel.SunDial.TimeChanged += (h,m) => timeService.SetTime(h, m, 0);
            sunTimePanel.SimulationSpeedControls.SpeedChanged += (hps) => timeService.SetTimeSpeed(hps * SecondsPerHour);
            sunTimePanel.SimulationSpeedControls.PlayToggled += timeService.ToggleAnimation;
            sunTimePanel.DateField.SubmitEvent += OnDateChanged;
            sunTimePanel.TimeChanged.AddListener(OnTimeChanged);
            
            timeService.timeSpeedChanged.AddListener(OnTimeSpeedChanged);
            timeService.timeOfDayChanged.AddListener(sunTimePanel.OnTimeOfDayChanged);
        }
        
        private void OnClosePanel()
        {
            timeService.timeSpeedChanged.RemoveListener(OnTimeSpeedChanged);
            timeService.timeOfDayChanged.RemoveListener(sunTimePanel.OnTimeOfDayChanged);
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
