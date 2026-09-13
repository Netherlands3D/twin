using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// Context-sensitive timeline shown for the selected timeline-enabled GeoJSON layer.
    /// Traffic layers receive vehicle/day filters and a dynamic intensity legend.
    /// </summary>
    [UxmlElement]
    public partial class TimelineSlider : VisualElement
    {
        private const long PlaybackIntervalMilliseconds = 2000;
        private static readonly float[] LegendStops = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        private readonly List<string> vehicleKeys = new();
        private readonly List<string> dayTypeKeys = new();
        private readonly VisualElement[] legendColors = new VisualElement[5];
        private readonly Label[] legendLabels = new Label[5];

        private SunTime sunTime;
        private TimelineGeoJson activeTimeline;
        private VisualElement expandedView;
        private VisualElement collapsedView;
        private VisualElement trafficControls;
        private VisualElement trafficLegend;
        private VisualElement trafficTicks;
        private DropDown vehicleDropdown;
        private DropDown dayTypeDropdown;
        private Slider slider;
        private Label titleLabel;
        private Label layerLabel;
        private Label currentTimeLabel;
        private Label routeCountLabel;
        private Label legendScaleLabel;
        private Label loadingLabel;
        private Button previousHourButton;
        private Button playbackButton;
        private Button nextHourButton;
        private IVisualElementScheduledItem playbackSchedule;

        private bool isAttached;
        private bool isUpdatingUi;
        private bool userCollapsed;
        private bool isPlaying;

        private DateTime minDateTime;
        public DateTime MinDateTime => minDateTime;

        [UxmlAttribute("min-datetime")]
        public string MinDateTimeString
        {
            get => minDateTime.ToString("O");
            set => minDateTime = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        private DateTime maxDateTime;
        public DateTime MaxDateTime => maxDateTime;

        [UxmlAttribute("max-datetime")]
        public string MaxDateTimeString
        {
            get => maxDateTime.ToString("O");
            set => maxDateTime = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        public TimelineSlider()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            expandedView = this.Q<VisualElement>("ExpandedView");
            collapsedView = this.Q<VisualElement>("CollapsedView");
            trafficControls = this.Q<VisualElement>("TrafficControls");
            trafficLegend = this.Q<VisualElement>("TrafficLegend");
            trafficTicks = this.Q<VisualElement>("TrafficTicks");

            vehicleDropdown = this.Q<DropDown>("VehicleDropdown");
            dayTypeDropdown = this.Q<DropDown>("DayTypeDropdown");
            slider = this.Q<Slider>("Timeline");

            titleLabel = this.Q<Label>("TitleLabel");
            layerLabel = this.Q<Label>("LayerLabel");
            currentTimeLabel = this.Q<Label>("CurrentTimeLabel");
            routeCountLabel = this.Q<Label>("RouteCountLabel");
            legendScaleLabel = this.Q<Label>("LegendScaleLabel");
            loadingLabel = this.Q<Label>("LoadingLabel");

            previousHourButton = this.Q<Button>("PreviousHourButton");
            playbackButton = this.Q<Button>("PlaybackButton");
            nextHourButton = this.Q<Button>("NextHourButton");
            var closeButton = this.Q<Button>("CloseButton");
            var openButton = this.Q<Button>("OpenButton");

            for (var i = 0; i < LegendStops.Length; i++)
            {
                legendColors[i] = this.Q<VisualElement>($"LegendColor{i}");
                legendLabels[i] = this.Q<Label>($"LegendLabel{i}");
                if (legendColors[i] != null)
                    legendColors[i].style.backgroundColor = TimelineGeoJson.GetTrafficColor(LegendStops[i]);
            }

            // During a Unity hot reload the C# type can be constructed one import tick before its UXML dependency.
            // Returning cleanly keeps the entire HUD from failing to clone; Unity recreates it after the UXML import.
            if (expandedView == null || collapsedView == null || trafficControls == null || trafficLegend == null
                || trafficTicks == null || vehicleDropdown == null || dayTypeDropdown == null || slider == null
                || titleLabel == null || layerLabel == null || currentTimeLabel == null || routeCountLabel == null
                || legendScaleLabel == null || loadingLabel == null || previousHourButton == null
                || playbackButton == null || nextHourButton == null || closeButton == null || openButton == null
                || legendColors.Any(element => element == null) || legendLabels.Any(element => element == null))
            {
                Debug.LogWarning("TimelineSlider UXML is not available yet; waiting for Unity to finish importing it.");
                return;
            }

            slider.RegisterValueChangedCallback(OnSliderChanged);
            vehicleDropdown.DropDownValueChanged.AddListener(OnVehicleChanged);
            dayTypeDropdown.DropDownValueChanged.AddListener(OnDayTypeChanged);
            previousHourButton.clicked += () => StepTime(-1);
            playbackButton.clicked += TogglePlayback;
            nextHourButton.clicked += () => StepTime(1);
            closeButton.clicked += Collapse;
            openButton.clicked += Expand;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            if (!Application.isPlaying || isAttached)
                return;

            isAttached = true;
            sunTime = ServiceLocator.GetService<SunTime>();
            TimelineGeoJson.ActiveTimelineChanged += OnActiveTimelineChanged;
            OnActiveTimelineChanged(TimelineGeoJson.ActiveTimeline);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            if (!isAttached)
                return;

            isAttached = false;
            StopPlayback();
            TimelineGeoJson.ActiveTimelineChanged -= OnActiveTimelineChanged;
            SetActiveTimeline(null);
        }

        private void OnActiveTimelineChanged(TimelineGeoJson timeline)
        {
            userCollapsed = false;
            SetActiveTimeline(timeline);
        }

        private void SetActiveTimeline(TimelineGeoJson timeline)
        {
            if (activeTimeline != timeline)
                StopPlayback();

            if (activeTimeline != null)
                activeTimeline.TimelineStateChanged -= OnTimelineStateChanged;

            activeTimeline = timeline;

            if (activeTimeline != null)
                activeTimeline.TimelineStateChanged += OnTimelineStateChanged;

            Refresh();
        }

        private void OnTimelineStateChanged(TimelineGeoJson _)
        {
            Refresh();
        }

        private void Collapse()
        {
            StopPlayback();
            userCollapsed = true;
            ApplyVisibility();
        }

        private void Expand()
        {
            userCollapsed = false;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            var isAvailable = activeTimeline != null && activeTimeline.HasTimelineData;
            EnableInClassList(UtilityClassConstants.HIDDEN, !isAvailable);
            expandedView.EnableInClassList(UtilityClassConstants.HIDDEN, !isAvailable || userCollapsed);
            collapsedView.EnableInClassList(UtilityClassConstants.HIDDEN, !isAvailable || !userCollapsed);
        }

        private void Refresh()
        {
            ApplyVisibility();
            if (activeTimeline == null || !activeTimeline.HasTimelineData)
                return;

            isUpdatingUi = true;
            var isTraffic = activeTimeline.HasTrafficData;

            titleLabel.text = isTraffic ? "Verkeersintensiteit" : "Tijdlijn";
            layerLabel.text = activeTimeline.LayerName;
            trafficControls.EnableInClassList(UtilityClassConstants.HIDDEN, !isTraffic);
            trafficLegend.EnableInClassList(UtilityClassConstants.HIDDEN, !isTraffic);
            trafficTicks.EnableInClassList(UtilityClassConstants.HIDDEN, !isTraffic);
            loadingLabel.EnableInClassList(
                UtilityClassConstants.HIDDEN,
                !isTraffic || activeTimeline.ParsingComplete);

            if (isTraffic)
            {
                if (isPlaying && activeTimeline.CurrentHour >= 23)
                    StopPlayback();

                PopulateTrafficDropdowns();
                slider.lowValue = 0f;
                slider.highValue = 23f;
                slider.SetValueWithoutNotify(activeTimeline.CurrentHour);
                currentTimeLabel.text = $"{activeTimeline.CurrentHour:00}:00 – {activeTimeline.CurrentHour:00}:59";
                routeCountLabel.text = activeTimeline.ParsingComplete
                    ? activeTimeline.VisibleRouteCount.ToString(CultureInfo.InvariantCulture)
                    : "–";
                UpdateLegend();

                previousHourButton.SetEnabled(activeTimeline.CurrentHour > 0);
                nextHourButton.SetEnabled(activeTimeline.CurrentHour < 23);
                playbackButton.SetEnabled(activeTimeline.ParsingComplete && (isPlaying || activeTimeline.CurrentHour < 23));
            }
            else
            {
                StopPlayback();
                EnsureLegacyDateRange();
                slider.lowValue = 0f;
                slider.highValue = 1f;
                var selectedTime = sunTime?.Time ?? minDateTime;
                slider.SetValueWithoutNotify(DateTimeToSliderValue(selectedTime));
                currentTimeLabel.text = selectedTime.ToString("dd/MM/yyyy HH:mm");
                previousHourButton.SetEnabled(selectedTime > minDateTime);
                nextHourButton.SetEnabled(selectedTime < maxDateTime);
                playbackButton.SetEnabled(false);
            }

            isUpdatingUi = false;
        }

        private void PopulateTrafficDropdowns()
        {
            vehicleKeys.Clear();
            vehicleKeys.AddRange(activeTimeline.AvailableVehicleTypes);
            vehicleDropdown.choices = vehicleKeys
                .Select(TimelineGeoJson.GetVehicleDisplayName)
                .ToList();

            var vehicleIndex = Math.Max(0, vehicleKeys.FindIndex(value =>
                string.Equals(value, activeTimeline.SelectedVehicleType, StringComparison.OrdinalIgnoreCase)));
            if (vehicleDropdown.choices.Count > 0)
                vehicleDropdown.SetValue(vehicleIndex);

            dayTypeKeys.Clear();
            dayTypeKeys.AddRange(activeTimeline.AvailableDayTypes);
            dayTypeDropdown.choices = dayTypeKeys
                .Select(TimelineGeoJson.GetDayTypeDisplayName)
                .ToList();

            var dayTypeIndex = Math.Max(0, dayTypeKeys.FindIndex(value =>
                string.Equals(value, activeTimeline.SelectedDayType, StringComparison.OrdinalIgnoreCase)));
            if (dayTypeDropdown.choices.Count > 0)
                dayTypeDropdown.SetValue(dayTypeIndex);
        }

        private void UpdateLegend()
        {
            var maximum = activeTimeline.GetScaleMaximumForSelectedVehicle();
            legendScaleLabel.text = maximum > 0f
                ? $"voertuigen/uur · schaal tot P95 ({FormatCount(maximum)})"
                : "voertuigen/uur";

            for (var i = 0; i < LegendStops.Length; i++)
            {
                var count = activeTimeline.GetCountAtNormalizedIntensity(LegendStops[i]);
                legendLabels[i].text = i == LegendStops.Length - 1
                    ? $"≥ {FormatCount(count)}"
                    : FormatCount(count);
            }
        }

        private static string FormatCount(float count)
        {
            return count < 10f && !Mathf.Approximately(count, Mathf.Round(count))
                ? count.ToString("0.0", CultureInfo.InvariantCulture)
                : count.ToString("0", CultureInfo.InvariantCulture);
        }

        private void OnVehicleChanged(int index)
        {
            if (isUpdatingUi || activeTimeline == null || index < 0 || index >= vehicleKeys.Count)
                return;

            activeTimeline.SelectVehicleType(vehicleKeys[index]);
        }

        private void OnDayTypeChanged(int index)
        {
            if (isUpdatingUi || activeTimeline == null || index < 0 || index >= dayTypeKeys.Count)
                return;

            activeTimeline.SelectDayType(dayTypeKeys[index]);
        }

        private void OnSliderChanged(ChangeEvent<float> evt)
        {
            if (isUpdatingUi || activeTimeline == null || sunTime == null)
                return;

            if (activeTimeline.HasTrafficData)
            {
                var hour = Mathf.Clamp(Mathf.RoundToInt(evt.newValue), 0, 23);
                slider.SetValueWithoutNotify(hour);
                sunTime.SetTime(hour, 0, 0);
                return;
            }

            SetLegacyDate(SliderValueToDateTime(evt.newValue));
        }

        private void StepTime(int direction)
        {
            if (activeTimeline == null)
                return;

            if (activeTimeline.HasTrafficData)
            {
                slider.value = Mathf.Clamp(activeTimeline.CurrentHour + direction, 0, 23);
                return;
            }

            EnsureLegacyDateRange();
            var selectedTime = (sunTime?.Time ?? minDateTime).AddHours(direction);
            selectedTime = selectedTime < minDateTime ? minDateTime : selectedTime;
            selectedTime = selectedTime > maxDateTime ? maxDateTime : selectedTime;
            SetLegacyDate(selectedTime);
            slider.SetValueWithoutNotify(DateTimeToSliderValue(selectedTime));
        }

        private void TogglePlayback()
        {
            if (isPlaying)
            {
                StopPlayback();
                return;
            }

            StartPlayback();
        }

        private void StartPlayback()
        {
            if (!isAttached || userCollapsed || activeTimeline == null || !activeTimeline.HasTrafficData
                || !activeTimeline.ParsingComplete || activeTimeline.CurrentHour >= 23)
                return;

            isPlaying = true;
            UpdatePlaybackButton();

            playbackSchedule?.Pause();
            playbackSchedule = schedule.Execute(AdvancePlayback)
                .StartingIn(PlaybackIntervalMilliseconds)
                .Every(PlaybackIntervalMilliseconds);
        }

        private void StopPlayback()
        {
            playbackSchedule?.Pause();
            playbackSchedule = null;

            if (!isPlaying)
                return;

            isPlaying = false;
            UpdatePlaybackButton();
        }

        private void AdvancePlayback()
        {
            if (!isPlaying || activeTimeline == null || userCollapsed || activeTimeline.CurrentHour >= 23)
            {
                StopPlayback();
                return;
            }

            StepTime(1);
        }

        private void UpdatePlaybackButton()
        {
            if (playbackButton == null)
                return;

            playbackButton.Image = isPlaying ? "Pause" : "Play";
            playbackButton.tooltip = isPlaying
                ? "Pauzeren"
                : "Afspelen · 1 uur per 2 seconden";
            playbackButton.EnableInClassList("timeline-slider__play-button--active", isPlaying);
        }

        private void EnsureLegacyDateRange()
        {
            if (minDateTime == default)
                minDateTime = DateTime.Today;
            if (maxDateTime <= minDateTime)
                maxDateTime = minDateTime.AddDays(1).AddSeconds(-1);
        }

        private DateTime SliderValueToDateTime(float value)
        {
            EnsureLegacyDateRange();
            return minDateTime.AddSeconds((maxDateTime - minDateTime).TotalSeconds * Mathf.Clamp01(value));
        }

        private float DateTimeToSliderValue(DateTime dateTime)
        {
            EnsureLegacyDateRange();
            return Mathf.Clamp01((float)((dateTime - minDateTime).TotalSeconds / (maxDateTime - minDateTime).TotalSeconds));
        }

        private void SetLegacyDate(DateTime dateTime)
        {
            if (sunTime == null)
                return;

            sunTime.SetDate(dateTime.Day, dateTime.Month, dateTime.Year);
            sunTime.SetTime(dateTime.Hour, dateTime.Minute, dateTime.Second);
            currentTimeLabel.text = dateTime.ToString("dd/MM/yyyy HH:mm");
        }
    }
}
