using System;
using Netherlands3D.Sun;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement, InspectorPanel]
    public partial class SunTimePanel : BaseInspectorContentPanel
    {
        public override string Title => "Zonpositie";

        // SunTime stores speed as seconds/second internally.
        // The UI shows speed in hours/second so we apply this factor.
        private const float SecondsPerHour = 3600f;

        private SunTime sunTime;

        private DateField dateField;
        private DateField DateField => dateField ??= this.Q<DateField>("DateField");

        private SunDial sunDial;
        private SunDial SunDial => sunDial ??= this.Q<SunDial>("SunDial");

        private NumberField timeField;
        private NumberField TimeField => timeField ??= this.Q<NumberField>("TimeField");

        private Button nowButton;
        private Button NowButton => nowButton ??= this.Q<Button>("NowButton");

        private SimulationSpeedControls simulationSpeedControls;
        private SimulationSpeedControls SimulationSpeedControls => simulationSpeedControls ??= this.Q<SimulationSpeedControls>("SimulationSpeedControls");

       

        public SunTimePanel()
        {
            sunTime = Services.ServiceLocator.GetService<SunTime>();
            
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            SunDial.TimeChanged += OnSunDialTimeChanged;
            DateField.SubmitEvent += OnDateChanged;
            TimeField.InputField.RegisterValueChangedCallback(OnTimeChanged);

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
            TimeField.SetValueWithoutNotify(dt);
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

        private void OnTimeChanged(ChangeEvent<string> _)
        {
            UnityEngine.Debug.Log($"Time: {sunTime.Time}");
            var time = timeField.GetValueAsTime();
            sunTime?.SetTime(time.Hour, time.Minute, 0);
        }

        private void OnDateChanged(int day, int month, int year)
        {
            sunTime?.SetDay(day);
            sunTime?.SetMonth(month);
            sunTime?.SetYear(year);
        }
    }
}