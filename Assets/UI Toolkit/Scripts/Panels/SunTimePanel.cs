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
        private static readonly float[] SpeedStepsHours = { 0.5f, 1f, 2f, 3f };

        private SunTime sunTime;

        // Date input
        private DateField dateField;
        private DateField DateField => dateField ??= this.Q<DateField>("DateField");

        // Time input
        private TimeField timeField;
        private TimeField TimeField => timeField ??= this.Q<TimeField>("TimeField");

        // Reset button
        private Button nowButton;
        private Button NowButton => nowButton ??= this.Q<Button>("NowButton");

        // Speed input (displayed in hours/second)
        private NumberField speedField;
        private NumberField SpeedField => speedField ??= this.Q<NumberField>("SpeedField");

        // Playback control buttons
        private Button slowDownButton;
        private Button SlowDownButton => slowDownButton ??= this.Q<Button>("SlowDownButton");

        private Button pauseButton;
        private Button PauseButton => pauseButton ??= this.Q<Button>("PauseButton");

        private Button playButton;
        private Button PlayButton => playButton ??= this.Q<Button>("PlayButton");

        private Button speedUpButton;
        private Button SpeedUpButton => speedUpButton ??= this.Q<Button>("SpeedUpButton");

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
            TimeField.ValueChanged += OnTimeChanged;
            SpeedField.InputField.RegisterValueChangedCallback(_ => OnSpeedChanged());

            NowButton.RegisterCallback<ClickEvent>(_ => sunTime?.ResetToNow());

            SlowDownButton.RegisterCallback<ClickEvent>(_ => StepTimeSpeed(increase: false));
            SpeedUpButton.RegisterCallback<ClickEvent>(_ => StepTimeSpeed(increase: true));
            PauseButton.RegisterCallback<ClickEvent>(_ => sunTime?.ToggleAnimation(false));
            PlayButton.RegisterCallback<ClickEvent>(_ => sunTime?.ToggleAnimation(true));
        }

        private void OnHideAction()
        {
            EnableInClassList("active", false);
            Unsubscribe();
        }

        private void OnShowAction()
        {
            EnableInClassList("active", true);
            Subscribe();
            LoadInitialValues();
        }

        private void Subscribe()
        {
            if (sunTime == null) return;
            sunTime.timeOfDayChanged.AddListener(OnTimeOfDayChanged);
            sunTime.timeSpeedChanged.AddListener(OnTimeSpeedChanged);
            sunTime.isAnimatingChanged.AddListener(OnIsAnimatingChanged);
        }

        private void Unsubscribe()
        {
            if (sunTime == null) return;
            sunTime.timeOfDayChanged.RemoveListener(OnTimeOfDayChanged);
            sunTime.timeSpeedChanged.RemoveListener(OnTimeSpeedChanged);
            sunTime.isAnimatingChanged.RemoveListener(OnIsAnimatingChanged);
        }

        private void LoadInitialValues()
        {
            if (sunTime == null) return;
            OnTimeOfDayChanged(sunTime.Time);
            OnIsAnimatingChanged(sunTime.IsAnimating);
            // Note: SunTime does not expose TimeSpeed publicly.
            // The speed field will populate on the first timeSpeedChanged event.
        }

        private void OnTimeOfDayChanged(DateTime dt)
        {
            DateField.SetValueWithoutNotify(dt.Day, dt.Month, dt.Year);
            TimeField.SetValueWithoutNotify(dt.ToString("HH:mm"));
        }

        /// <param name="speedSecondsPerSecond">Internal SunTime speed (seconds of sim-time per real second).</param>
        private void OnTimeSpeedChanged(float speedSecondsPerSecond)
        {
            var hoursPerSecond = speedSecondsPerSecond / SecondsPerHour;
            SpeedField.SetValueWithoutNotify(hoursPerSecond);
        }

        private void OnIsAnimatingChanged(bool animating)
        {
            // Highlight the button that represents the current state
            PauseButton.EnableInClassList("sun-time-panel__control-button--active", animating);
            PlayButton.EnableInClassList("sun-time-panel__control-button--active", !animating);
        }

        private void OnDateChanged(int day, int month, int year)
        {
            if (sunTime == null) return;
            sunTime.SetDay(day);
            sunTime.SetMonth(month);
            sunTime.SetYear(year);
        }

        private void OnTimeChanged(string value)
        {
            if (sunTime == null) return;
            var text = value
                .Replace('.', ':')
                .Replace(';', ':')
                .Replace(',', ':');
            if (DateTime.TryParse(text, out var parsed))
                sunTime.SetTime(parsed.Hour, parsed.Minute, 0);
        }

        private void OnSpeedChanged()
        {
            if (sunTime == null) return;
            var hoursPerSecond = (float)SpeedField.GetValueAsDouble();
            sunTime.SetTimeSpeed(hoursPerSecond * SecondsPerHour);
        }

        private void StepTimeSpeed(bool increase)
        {
            if (sunTime == null) return;

            var current = (float)SpeedField.GetValueAsDouble();
            var target = increase ? GetNextSpeedStep(current) : GetPreviousSpeedStep(current);

            SpeedField.SetValueWithoutNotify(target);
            sunTime.SetTimeSpeed(target * SecondsPerHour);
        }

        private static float GetNextSpeedStep(float current)
        {
            for (var i = 0; i < SpeedStepsHours.Length; i++)
            {
                if (current < SpeedStepsHours[i])
                    return SpeedStepsHours[i];
            }

            return SpeedStepsHours[SpeedStepsHours.Length - 1];
        }

        private static float GetPreviousSpeedStep(float current)
        {
            for (var i = SpeedStepsHours.Length - 1; i >= 0; i--)
            {
                if (current > SpeedStepsHours[i])
                    return SpeedStepsHours[i];
            }

            return SpeedStepsHours[0];
        }
    }
}