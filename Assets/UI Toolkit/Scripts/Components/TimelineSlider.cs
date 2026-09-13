using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin;
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
        private const float PlaybackSecondsPerSecond = 1800f;
        private static readonly float[] LegendStops = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        private readonly List<string> vehicleKeys = new();
        private readonly List<string> dayTypeKeys = new();
        private readonly VisualElement[] legendColors = new VisualElement[5];
        private readonly Label[] legendLabels = new Label[5];
        private readonly List<WorldLabelElement> worldLabelElements = new();

        private SunTime sunTime;
        private TimelineGeoJson activeTimeline;
        private VisualElement expandedView;
        private VisualElement collapsedView;
        private VisualElement trafficControls;
        private VisualElement trafficLegend;
        private VisualElement trafficTicks;
        private VisualElement presentationControls;
        private VisualElement timeControls;
        private VisualElement worldLabelOverlay;
        private DropDown vehicleDropdown;
        private DropDown dayTypeDropdown;
        private DropDown metricDropdown;
        private Slider slider;
        private Icon headerIcon;
        private Label titleLabel;
        private Label layerLabel;
        private Label currentTimeLabel;
        private Label playbackStatusLabel;
        private Label routeCountLabel;
        private Label legendScaleLabel;
        private Label legendTitleLabel;
        private Label legendExplanationLabel;
        private Label loadingLabel;
        private Label presentationContextLabel;
        private Label featureCountLabel;
        private Label featureCaptionLabel;
        private Button previousHourButton;
        private Button playbackButton;
        private Button nextHourButton;
        private Button openButton;
        private CheckboxToggle worldLabelsToggle;
        private CheckboxToggle trafficWorldLabelsToggle;
        private IVisualElementScheduledItem worldLabelUpdate;
        private bool rebuildingWorldLabels;

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
            presentationControls = this.Q<VisualElement>("DataPresentationControls");
            timeControls = this.Q<VisualElement>("TimeControls");

            vehicleDropdown = this.Q<DropDown>("VehicleDropdown");
            dayTypeDropdown = this.Q<DropDown>("DayTypeDropdown");
            metricDropdown = this.Q<DropDown>("MetricDropdown");
            slider = this.Q<Slider>("Timeline");

            headerIcon = this.Q<Icon>("HeaderIcon");
            titleLabel = this.Q<Label>("TitleLabel");
            layerLabel = this.Q<Label>("LayerLabel");
            currentTimeLabel = this.Q<Label>("CurrentTimeLabel");
            playbackStatusLabel = this.Q<Label>("PlaybackStatusLabel");
            routeCountLabel = this.Q<Label>("RouteCountLabel");
            legendScaleLabel = this.Q<Label>("LegendScaleLabel");
            legendTitleLabel = this.Q<Label>("LegendTitleLabel");
            legendExplanationLabel = this.Q<Label>("LegendExplanation");
            loadingLabel = this.Q<Label>("LoadingLabel");
            presentationContextLabel = this.Q<Label>("PresentationContextLabel");
            featureCountLabel = this.Q<Label>("FeatureCountLabel");
            featureCaptionLabel = this.Q<Label>("FeatureCaptionLabel");

            previousHourButton = this.Q<Button>("PreviousHourButton");
            playbackButton = this.Q<Button>("PlaybackButton");
            nextHourButton = this.Q<Button>("NextHourButton");
            worldLabelsToggle = this.Q<CheckboxToggle>("WorldLabelsToggle");
            trafficWorldLabelsToggle = this.Q<CheckboxToggle>("TrafficWorldLabelsToggle");
            var closeButton = this.Q<Button>("CloseButton");
            openButton = this.Q<Button>("OpenButton");

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
                || trafficTicks == null || presentationControls == null || timeControls == null
                || vehicleDropdown == null || dayTypeDropdown == null || metricDropdown == null || slider == null
                || headerIcon == null || titleLabel == null || layerLabel == null || currentTimeLabel == null
                || playbackStatusLabel == null || routeCountLabel == null || legendScaleLabel == null
                || legendTitleLabel == null || legendExplanationLabel == null || loadingLabel == null
                || presentationContextLabel == null || featureCountLabel == null || featureCaptionLabel == null
                || previousHourButton == null || worldLabelsToggle == null || trafficWorldLabelsToggle == null
                || playbackButton == null || nextHourButton == null || closeButton == null || openButton == null
                || legendColors.Any(element => element == null) || legendLabels.Any(element => element == null))
            {
                Debug.LogWarning("TimelineSlider UXML is not available yet; waiting for Unity to finish importing it.");
                return;
            }

            slider.RegisterValueChangedCallback(OnSliderChanged);
            vehicleDropdown.DropDownValueChanged.AddListener(OnVehicleChanged);
            dayTypeDropdown.DropDownValueChanged.AddListener(OnDayTypeChanged);
            metricDropdown.DropDownValueChanged.AddListener(OnMetricChanged);
            worldLabelsToggle.RegisterValueChangedCallback(OnWorldLabelsChanged);
            trafficWorldLabelsToggle.RegisterValueChangedCallback(OnWorldLabelsChanged);
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
            CreateWorldLabelOverlay();
            worldLabelUpdate = schedule.Execute(UpdateWorldLabelPositions).Every(100);
            TimelineGeoJson.ActiveTimelineChanged += OnActiveTimelineChanged;
            TimelineGeoJson.WorldLabelStateChanged += OnWorldLabelStateChanged;
            OnActiveTimelineChanged(TimelineGeoJson.ActiveTimeline);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            if (!isAttached)
                return;

            isAttached = false;
            StopPlayback();
            TimelineGeoJson.ActiveTimelineChanged -= OnActiveTimelineChanged;
            TimelineGeoJson.WorldLabelStateChanged -= OnWorldLabelStateChanged;
            SetActiveTimeline(null);
            worldLabelUpdate?.Pause();
            worldLabelUpdate = null;
            worldLabelOverlay?.RemoveFromHierarchy();
            worldLabelOverlay = null;
            worldLabelElements.Clear();
        }

        private void OnActiveTimelineChanged(TimelineGeoJson timeline)
        {
            userCollapsed = false;
            SetWorldLabelToggleValues(timeline?.WorldLabelsEnabled == true);
            SetActiveTimeline(timeline);
        }

        private void OnWorldLabelStateChanged()
        {
            SetWorldLabelToggleValues(activeTimeline?.WorldLabelsEnabled == true);
            RebuildWorldLabels();
            ApplyWorldLabelVisibility();
        }

        private void SetWorldLabelToggleValues(bool enabled)
        {
            worldLabelsToggle?.SetValueWithoutNotify(enabled);
            trafficWorldLabelsToggle?.SetValueWithoutNotify(enabled);
        }

        private void SetActiveTimeline(TimelineGeoJson timeline)
        {
            if (activeTimeline != timeline)
                StopPlayback();

            if (activeTimeline != null)
            {
                activeTimeline.TimelineStateChanged -= OnTimelineStateChanged;
                activeTimeline.TimelineTimeChanged -= OnTimelineTimeChanged;
            }

            activeTimeline = timeline;

            if (activeTimeline != null)
            {
                activeTimeline.TimelineStateChanged += OnTimelineStateChanged;
                activeTimeline.TimelineTimeChanged += OnTimelineTimeChanged;
            }

            Refresh();
        }

        private void OnTimelineStateChanged(TimelineGeoJson _)
        {
            Refresh();
        }

        private void OnTimelineTimeChanged(TimelineGeoJson timeline)
        {
            if (timeline == activeTimeline && timeline.HasTrafficData)
                UpdateTrafficTimeReadout();
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
            var isAvailable = activeTimeline != null && activeTimeline.HasContextualData;
            EnableInClassList(UtilityClassConstants.HIDDEN, !isAvailable);
            expandedView.EnableInClassList(UtilityClassConstants.HIDDEN, !isAvailable || userCollapsed);
            collapsedView.EnableInClassList(UtilityClassConstants.HIDDEN, !isAvailable || !userCollapsed);
            ApplyWorldLabelVisibility();
        }

        private void Refresh()
        {
            ApplyVisibility();
            if (activeTimeline == null || !activeTimeline.HasContextualData)
            {
                RebuildWorldLabels();
                return;
            }

            isUpdatingUi = true;
            var isTraffic = activeTimeline.HasTrafficData;
            var isPresentation = activeTimeline.HasPresentationData && !isTraffic;

            titleLabel.text = isPresentation
                ? activeTimeline.PresentationTitle
                : isTraffic ? "Verkeersintensiteit" : "Tijdlijn";
            layerLabel.text = activeTimeline.LayerName;
            headerIcon.Image = isPresentation ? "PresentationChart" : "Clock";
            openButton.Image = isPresentation ? "PresentationChart" : "Clock";
            openButton.LabelText = isPresentation ? "Dataweergave tonen" : "Tijdlijn tonen";
            trafficControls.EnableInClassList(UtilityClassConstants.HIDDEN, !isTraffic);
            presentationControls.EnableInClassList(UtilityClassConstants.HIDDEN, !isPresentation);
            timeControls.EnableInClassList(UtilityClassConstants.HIDDEN, isPresentation);
            slider.EnableInClassList(UtilityClassConstants.HIDDEN, isPresentation);
            trafficLegend.EnableInClassList(UtilityClassConstants.HIDDEN, !isTraffic && !isPresentation);
            trafficTicks.EnableInClassList(UtilityClassConstants.HIDDEN, !isTraffic);
            loadingLabel.text = isPresentation ? "GeoJSON-weergave wordt voorbereid…" : "Verkeersgegevens worden verwerkt…";
            loadingLabel.EnableInClassList(
                UtilityClassConstants.HIDDEN,
                activeTimeline.ParsingComplete);

            if (isPresentation)
            {
                StopPlayback();
                PopulatePresentationControls();
                UpdatePresentationLegend();
                RebuildWorldLabels();
                isUpdatingUi = false;
                return;
            }

            if (isTraffic)
            {
                if (isPlaying && activeTimeline.CurrentHour >= 23)
                {
                    StopPlayback();
                    var endTime = activeTimeline.CurrentTime.Date.AddHours(23);
                    if (activeTimeline.CurrentTime > endTime)
                        SetTrafficTime(endTime);
                }

                PopulateTrafficDropdowns();
                slider.lowValue = 0f;
                slider.highValue = 23f;
                UpdateTrafficTimeReadout();
                routeCountLabel.text = activeTimeline.ParsingComplete
                    ? activeTimeline.VisibleRouteCount.ToString(CultureInfo.InvariantCulture)
                    : "–";
                UpdateLegend();
                RebuildWorldLabels();

                previousHourButton.SetEnabled(GetTrafficSliderValue(activeTimeline.CurrentTime) > 0f);
                nextHourButton.SetEnabled(activeTimeline.CurrentHour < 23);
                playbackButton.SetEnabled(activeTimeline.ParsingComplete
                                          && (isPlaying || GetTrafficSliderValue(activeTimeline.CurrentTime) < 23f));
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

        private void PopulatePresentationControls()
        {
            metricDropdown.choices = activeTimeline.PresentationMetricNames.ToList();
            var metricIndex = Math.Max(0, metricDropdown.choices.FindIndex(value =>
                string.Equals(value, activeTimeline.SelectedPresentationMetric, StringComparison.OrdinalIgnoreCase)));
            if (metricDropdown.choices.Count > 0)
                metricDropdown.SetValue(metricIndex);

            presentationContextLabel.text = activeTimeline.PresentationContext;
            featureCountLabel.text = activeTimeline.ParsingComplete
                ? activeTimeline.PresentationFeatureCount.ToString(CultureInfo.InvariantCulture)
                : "–";
            featureCaptionLabel.text = activeTimeline.PresentationFeatureCaption;
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
            legendTitleLabel.text = "Relatieve verkeersintensiteit";
            legendExplanationLabel.text = "Kleur en lijndikte tonen de intensiteit; selecteer een route voor de exacte waarde.";
            var maximum = activeTimeline.GetScaleMaximumForSelectedVehicle();
            legendScaleLabel.text = maximum > 0f
                ? $"voertuigen/uur · schaal tot P95 ({FormatCount(maximum)})"
                : "voertuigen/uur";

            for (var i = 0; i < LegendStops.Length; i++)
            {
                legendColors[i].style.backgroundColor = TimelineGeoJson.GetTrafficColor(LegendStops[i]);
                var count = activeTimeline.GetCountAtNormalizedIntensity(LegendStops[i]);
                legendLabels[i].text = i == LegendStops.Length - 1
                    ? $"≥ {FormatCount(count)}"
                    : FormatCount(count);
            }
        }

        private void UpdatePresentationLegend()
        {
            legendTitleLabel.text = activeTimeline.PresentationLegendTitle;
            legendScaleLabel.text = activeTimeline.PresentationLegendScale;
            legendExplanationLabel.text = activeTimeline.PresentationLegendExplanation;

            for (var i = 0; i < LegendStops.Length; i++)
            {
                legendColors[i].style.backgroundColor = activeTimeline.GetPresentationLegendColor(LegendStops[i]);
                var value = activeTimeline.GetPresentationLegendValue(LegendStops[i]);
                var formatted = activeTimeline.PresentationKind is GeoJsonPresentationKind.RoadIntervention
                    or GeoJsonPresentationKind.RouteReference
                    ? FormatCount(value)
                    : value.ToString("0.0", CultureInfo.InvariantCulture);
                legendLabels[i].text = i == LegendStops.Length - 1 ? $"≥ {formatted}" : formatted;
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

        private void OnMetricChanged(int index)
        {
            if (isUpdatingUi || activeTimeline == null || !activeTimeline.HasPresentationData)
                return;

            activeTimeline.SelectPresentationMetric(index);
        }

        private void OnWorldLabelsChanged(ChangeEvent<bool> evt)
        {
            if (activeTimeline == null)
                return;

            activeTimeline.SetWorldLabelsEnabled(evt.newValue);
        }

        private void OnSliderChanged(ChangeEvent<float> evt)
        {
            if (isUpdatingUi || activeTimeline == null || sunTime == null)
                return;

            if (activeTimeline.HasTrafficData)
            {
                SetTrafficTime(evt.newValue);
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
            sunTime.SetTimeSpeed(PlaybackSecondsPerSecond);
            sunTime.ToggleAnimation(true);
            UpdatePlaybackButton();
        }

        private void StopPlayback()
        {
            if (!isPlaying)
                return;

            isPlaying = false;
            sunTime?.ToggleAnimation(false);
            UpdatePlaybackButton();
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

        private void UpdateTrafficTimeReadout()
        {
            if (activeTimeline == null)
                return;

            var selectedTime = activeTimeline.CurrentTime;
            var renderedHour = activeTimeline.RenderedTrafficHour >= 0
                ? activeTimeline.RenderedTrafficHour
                : selectedTime.Hour;
            slider.SetValueWithoutNotify(GetTrafficSliderValue(selectedTime));
            currentTimeLabel.text = selectedTime.ToString("HH:mm");
            playbackStatusLabel.text =
                $"Data: {renderedHour:00}:00 – {renderedHour:00}:59 · 1 uur per 2 sec";
        }

        private void SetTrafficTime(float timelineHour)
        {
            var totalSeconds = Mathf.RoundToInt(Mathf.Clamp(timelineHour, 0f, 23f) * 3600f);
            SetTrafficTime(activeTimeline.CurrentTime.Date.AddSeconds(totalSeconds));
        }

        private void SetTrafficTime(DateTime time)
        {
            sunTime.SetTime(time);
        }

        private static float GetTrafficSliderValue(DateTime time)
        {
            return time.Hour + time.Minute / 60f + time.Second / 3600f;
        }

        private void CreateWorldLabelOverlay()
        {
            if (worldLabelOverlay != null)
                return;

            var uiRoot = App.UIRoot?.Root;
            if (uiRoot == null)
                return;

            worldLabelOverlay = new VisualElement
            {
                name = "GeoJsonWorldLabelOverlay",
                pickingMode = PickingMode.Ignore
            };
            worldLabelOverlay.AddToClassList("geojson-world-label-overlay");
            var stylesheet = Resources.Load<StyleSheet>("UI/Components/TimelineSlider-style");
            if (stylesheet != null)
                worldLabelOverlay.styleSheets.Add(stylesheet);
            uiRoot.Add(worldLabelOverlay);
            worldLabelOverlay.BringToFront();

            ApplyWorldLabelVisibility();
        }

        private void RebuildWorldLabels()
        {
            if (rebuildingWorldLabels)
                return;

            rebuildingWorldLabels = true;
            try
            {
                worldLabelElements.Clear();
                worldLabelOverlay?.Clear();
                if (worldLabelOverlay == null)
                    return;

                foreach (var timeline in TimelineGeoJson.VisibleWorldLabelSources)
                {
                    foreach (var data in timeline.GetWorldLabels())
                    {
                        var label = new Label(data.Text)
                        {
                            tooltip = data.Tooltip,
                            pickingMode = PickingMode.Position
                        };
                        label.AddToClassList("geojson-world-label");
                        label.style.backgroundColor = data.Color;
                        label.style.color = data.TextColor;
                        label.style.width = 42f;
                        worldLabelOverlay.Add(label);
                        worldLabelElements.Add(new WorldLabelElement(label, data));
                    }
                }
            }
            finally
            {
                rebuildingWorldLabels = false;
            }
        }

        private void ApplyWorldLabelVisibility()
        {
            if (worldLabelOverlay == null)
                return;

            var visible = TimelineGeoJson.HasVisibleWorldLabelSources;
            worldLabelOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateWorldLabelPositions()
        {
            if (worldLabelOverlay == null)
            {
                CreateWorldLabelOverlay();
                ApplyWorldLabelVisibility();
            }

            if (worldLabelOverlay == null || worldLabelOverlay.resolvedStyle.display == DisplayStyle.None)
                return;

            if (worldLabelElements.Count == 0 && TimelineGeoJson.HasVisibleWorldLabelSources)
                RebuildWorldLabels();

            var activeCamera = App.Cameras?.ActiveCamera;
            var uiRoot = App.UIRoot;
            if (activeCamera == null || uiRoot?.Root == null)
                return;

            var rootBounds = uiRoot.Root.worldBound;
            var overlayOffset = worldLabelOverlay.worldBound.position;
            foreach (var item in worldLabelElements)
            {
                var screenPosition = activeCamera.WorldToScreenPoint(item.Data.WorldPosition);
                if (screenPosition.z <= 0f)
                {
                    item.Label.style.display = DisplayStyle.None;
                    continue;
                }

                var panelPosition = uiRoot.GetUIPositionFromScreenPosition(screenPosition);
                var onScreen = rootBounds.Contains(panelPosition);
                item.Label.style.display = onScreen ? DisplayStyle.Flex : DisplayStyle.None;
                if (!onScreen)
                    continue;

                var localPosition = panelPosition - overlayOffset;
                item.Label.style.left = localPosition.x - 21f;
                item.Label.style.top = localPosition.y - 12f;
            }
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

        private sealed class WorldLabelElement
        {
            public Label Label { get; }
            public GeoJsonWorldLabel Data { get; }

            public WorldLabelElement(Label label, GeoJsonWorldLabel data)
            {
                Label = label;
                Data = data;
            }
        }
    }
}
