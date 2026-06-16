using System;
using Netherlands3D.Sun;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using UnityEngine;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class SunTimePanel : BaseInspectorContentPanel
    {
        public override string Title => "Zonnestand";

        // SunTime stores speed as seconds/second internally.
        // The UI shows speed in hours/second so we apply this factor.
        private const float SecondsPerHour = 3600f;

        private SunTime sunTime;

        private DateField dateField;
        private DateField DateField => dateField ??= this.Q<DateField>("DateField");

        private SunDial sunDial;
        private SunDial SunDial => sunDial ??= this.Q<SunDial>("SunDial");

        private TimeField timeField;
        private TimeField TimeField => timeField ??= this.Q<TimeField>("TimeField");

        private Button nowButton;
        private Button NowButton => nowButton ??= this.Q<Button>("NowButton");

        private SimulationSpeedControls simulationSpeedControls;
        private SimulationSpeedControls SimulationSpeedControls => simulationSpeedControls ??= this.Q<SimulationSpeedControls>("SimulationSpeedControls");

        private ScreenshotContainer images;


        public SunTimePanel()
        {
        }

        public SunTimePanel(ScriptableObject imageContainer) : this()
        {
            sunTime = Services.ServiceLocator.GetService<SunTime>();
            
            if(imageContainer is not ScreenshotContainer screenshots)
                Debug.LogError("missing images for schaduwstudie, please provide a screenshotcontainer scriptableobject");
            else
                images = screenshots;

            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            SunDial.TimeChanged += OnSunDialTimeChanged;
            DateField.SubmitEvent += OnDateChanged;
            TimeField.TimeChanged += OnTimeChanged;

            NowButton.RegisterCallback<ClickEvent>(OnNowButtonClicked);

            SimulationSpeedControls.SpeedChanged += OnSimulationSpeedChanged;
            SimulationSpeedControls.PlayToggled += OnPlayToggled;
            
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                sunTime.timeOfDayChanged.AddListener(OnTimeOfDayChanged);
                sunTime.timeSpeedChanged.AddListener(OnTimeSpeedChanged);
                sunTime.isAnimatingChanged.AddListener(OnIsAnimatingChanged);
                OnTimeOfDayChanged(sunTime.Time);
                OnIsAnimatingChanged(sunTime.IsAnimating);    
            });
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                sunTime.timeOfDayChanged.RemoveListener(OnTimeOfDayChanged);
                sunTime.timeSpeedChanged.RemoveListener(OnTimeSpeedChanged);
                sunTime.isAnimatingChanged.RemoveListener(OnIsAnimatingChanged);
            });
        }

        void OnNowButtonClicked(ClickEvent _)
        {
            sunTime?.ResetToNow();
            SimulationSpeedControls.Pause();
        }

        private void OnTimeOfDayChanged(DateTime dt)
        {
            SunDial.SetTimeWithoutNotify(dt.Hour, dt.Minute);
            DateField.SetValueWithoutNotify(dt.Day, dt.Month, dt.Year);
            TimeField.SetValueWithoutNotify(dt.ToString("HH:mm"));
        }

        private void OnSunDialTimeChanged(int hour, int minute)
        {
            sunTime?.SetTime(hour, minute, 0);
        }

        private void OnIsAnimatingChanged(bool animating)
        {
            if (animating) SimulationSpeedControls.Play();
            else SimulationSpeedControls.Pause();
        }

        private void OnTimeSpeedChanged(float speedSecondsPerSecond)
        {
            SimulationSpeedControls.SetSpeedWithoutNotify(speedSecondsPerSecond / SecondsPerHour);
        }

        private void OnSimulationSpeedChanged(float hoursPerSecond)
        {
            sunTime?.SetTimeSpeed(hoursPerSecond * SecondsPerHour);
        }

        private void OnPlayToggled(bool isPlaying)
        {
            sunTime?.ToggleAnimation(isPlaying);
        }

        private void OnTimeChanged(int hour, int minute)
        {
            sunTime?.SetTime(hour, minute, 0);
        }

        private void OnDateChanged(int day, int month, int year)
        {
            sunTime?.SetDay(day);
            sunTime?.SetMonth(month);
            sunTime?.SetYear(year);
        }
    }
}