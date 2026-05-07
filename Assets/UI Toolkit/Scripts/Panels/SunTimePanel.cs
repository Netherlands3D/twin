using System;
using Netherlands3D.Sun;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class SunTimePanel : BaseInspectorContentPanel
    {
        public override string GetTitle() => "Zonpositie";

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

        /// <summary>Parameterless constructor required for UXML deserialization.</summary>
        public SunTimePanel()
        {
        }

        public SunTimePanel(SunTime sunTime)
        {
            this.sunTime = sunTime;

            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += OnShowAction;
            OnHide += OnHideAction;
            DateField.SubmitEvent += OnDateChanged;
            SunDial.TimeChanged += OnSunDialTimeChanged;
            TimeField.TimeChanged += OnTimeChanged;

            NowButton.RegisterCallback<ClickEvent>(_ => sunTime?.ResetToNow());

            SimulationSpeedControls.SpeedChanged += OnSimulationSpeedChanged;
            SimulationSpeedControls.PlayToggled += OnPlayToggled;
        }

        private void OnShowAction()
        {
            EnableInClassList("hidden", false);
            
            if (sunTime == null) return;
            sunTime.timeOfDayChanged.AddListener(OnTimeOfDayChanged);
            sunTime.timeSpeedChanged.AddListener(OnTimeSpeedChanged);
            sunTime.isAnimatingChanged.AddListener(OnIsAnimatingChanged);
            OnTimeOfDayChanged(sunTime.Time);
            OnIsAnimatingChanged(sunTime.IsAnimating);
        }

        private void OnHideAction()
        {
            EnableInClassList("hidden", true);
            
            if (sunTime == null) return;
            sunTime.timeOfDayChanged.RemoveListener(OnTimeOfDayChanged);
            sunTime.timeSpeedChanged.RemoveListener(OnTimeSpeedChanged);
            sunTime.isAnimatingChanged.RemoveListener(OnIsAnimatingChanged);
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
            SimulationSpeedControls.SetIsPlaying(animating);
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