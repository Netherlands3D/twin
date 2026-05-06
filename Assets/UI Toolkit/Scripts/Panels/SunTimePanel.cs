using System;
using Netherlands3D.Sun;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using TextField = Netherlands3D.UI.Components.TextField;

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

        // Date inputs
        private NumberField dayField;
        private NumberField DayField => dayField ??= this.Q<NumberField>("DayField");

        private NumberField monthField;
        private NumberField MonthField => monthField ??= this.Q<NumberField>("MonthField");

        private NumberField yearField;
        private NumberField YearField => yearField ??= this.Q<NumberField>("YearField");

        // Time input
        private TextField timeField;
        private TextField TimeField => timeField ??= this.Q<TextField>("TimeField");

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

        // Focus tracking — prevents SunTime events from overwriting a field while the user is editing it
        private bool dayFocused;
        private bool monthFocused;
        private bool yearFocused;
        private bool timeFocused;
        private bool speedFocused;

        /// <summary>Parameterless constructor required for UXML deserialization.</summary>
        public SunTimePanel()
        {
        }

        public SunTimePanel(SunTime sunTime)
        {
            this.sunTime = sunTime;

            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += () =>
            {
                EnableInClassList("active", true);
                Subscribe();
                LoadInitialValues();
            };

            OnHide += () =>
            {
                EnableInClassList("active", false);
                Unsubscribe();
            };

            DayField.RegisterCallback<FocusInEvent>(_ => dayFocused = true);
            DayField.RegisterCallback<FocusOutEvent>(_ =>
            {
                dayFocused = false;
                OnDayChanged();
            });
            DayField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => OnDayChanged(), TrickleDown.TrickleDown);

            MonthField.RegisterCallback<FocusInEvent>(_ => monthFocused = true);
            MonthField.RegisterCallback<FocusOutEvent>(_ =>
            {
                monthFocused = false;
                OnMonthChanged();
            });
            MonthField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => OnMonthChanged(),
                TrickleDown.TrickleDown);

            YearField.RegisterCallback<FocusInEvent>(_ => yearFocused = true);
            YearField.RegisterCallback<FocusOutEvent>(_ =>
            {
                yearFocused = false;
                OnYearChanged();
            });
            YearField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => OnYearChanged(), TrickleDown.TrickleDown);

            TimeField.RegisterCallback<FocusInEvent>(_ => timeFocused = true);
            TimeField.RegisterCallback<FocusOutEvent>(_ =>
            {
                timeFocused = false;
                OnTimeChanged();
            });
            TimeField.RegisterCallback<NavigationSubmitEvent>(_ => OnTimeChanged(), TrickleDown.TrickleDown);

            SpeedField.RegisterCallback<FocusInEvent>(_ => speedFocused = true);
            SpeedField.RegisterCallback<FocusOutEvent>(_ =>
            {
                speedFocused = false;
                OnSpeedChanged();
            });
            SpeedField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => OnSpeedChanged(),
                TrickleDown.TrickleDown);

            NowButton.RegisterCallback<ClickEvent>(_ => sunTime?.ResetToNow());

            SlowDownButton.RegisterCallback<ClickEvent>(_ => sunTime?.MultiplyTimeSpeed(0.1f));
            SpeedUpButton.RegisterCallback<ClickEvent>(_ => sunTime?.MultiplyTimeSpeed(10f));
            PauseButton.RegisterCallback<ClickEvent>(_ => sunTime?.ToggleAnimation(false));
            PlayButton.RegisterCallback<ClickEvent>(_ => sunTime?.ToggleAnimation(true));
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
            if (!dayFocused) DayField.SetValueWithoutNotify(dt.Day);
            if (!monthFocused) MonthField.SetValueWithoutNotify(dt.Month);
            if (!yearFocused) YearField.SetValueWithoutNotify(dt.Year);
            if (!timeFocused) TimeField.SetValueWithoutNotify(dt.ToString("HH:mm"));
        }

        /// <param name="speedSecondsPerSecond">Internal SunTime speed (seconds of sim-time per real second).</param>
        private void OnTimeSpeedChanged(float speedSecondsPerSecond)
        {
            if (!speedFocused)
            {
                var hoursPerSecond = speedSecondsPerSecond / SecondsPerHour;
                SpeedField.SetValueWithoutNotify(hoursPerSecond);
            }
        }

        private void OnIsAnimatingChanged(bool animating)
        {
            // Highlight the button that represents the current state
            PauseButton.EnableInClassList("sun-time-panel__control-button--active", animating);
            PlayButton.EnableInClassList("sun-time-panel__control-button--active", !animating);
        }

        private void OnDayChanged() => sunTime?.SetDay(DayField.GetValueAsInt());
        private void OnMonthChanged() => sunTime?.SetMonth(MonthField.GetValueAsInt());
        private void OnYearChanged() => sunTime?.SetYear(YearField.GetValueAsInt());

        private void OnTimeChanged()
        {
            if (sunTime == null) return;
            var text = TimeField.value
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
    }
}