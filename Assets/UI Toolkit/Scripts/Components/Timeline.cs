using System;
using System.Collections.Generic;
using System.Globalization;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Timeline;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Services;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Timeline : VisualElement
    {
        private Layers layersService;
        private readonly List<(CityJSONLayerGameObject Layer, TimelineCityJson Timeline)> timelineLayers = new();
        
        private readonly int scrollAreaWidthPx = 50;
        private double scrollAreaWidth = .5f;

        private double thumbScrollMinPosition = .5d;
        private double thumbScrollMaxPosition = .5d;
        
        private double thumbMinPosition = 0d;
        private double thumbMaxPosition = 1d;
        private double maxScrollSpeed = 1d;
       
        private readonly VisualElement marksContainer;
        private readonly List<VisualElement> markElements = new();
        
        private readonly VisualElement labelsContainer;
        private readonly List<DateTimeLabel> labelElements = new();
        
        private readonly VisualElement thumbElement;
        private readonly DateTimeLabel thumbLabelElement;
        
        private DateTime currentTime;
        private DateTime lastCurrentTime;
        private DateTime preciseCurrentTime;
        private DateTime displayedCurrentTime;
        private DateTime rootTime; //used for snapping
        private DateTime startTime;
        private DateTime endTime;
        private TimeSpan duration;
        private double thumbPosition;
        
        private double ticksPerPixel;
        private double timePerPixel;

        private DateTimeUnit unitContext;
        private DateTimeUnit unitPrecision;
        private DateTimeInterval snapInterval = new DateTimeInterval(DateTimeUnit.Year, 1);

        private DateTimeInterval marksInterval;
        private DateTimeInterval labelsInterval;

        private DateTime timelineDragInitialStartTime;
        private DateTime timelineDragInitialEndTime;

        private bool isThumbDrag;
        private double thumbDragInitialThumbElementPosition;
        private DateTime thumbDragInitialStartTime;
        private DateTime thumbDragInitialEndTime;
        private double thumbElementPosition;
        private readonly IVisualElementScheduledItem thumbDragUpdateItem;
        private TimeSpan thumbElementTimeOffset;

        private bool markElementsUpdateRequested = false;
        private bool unitGroupsUpdateRequested = false;

        private bool isSnapping;
        private TimeSpan snapStartedThumbElementTimeOffset;

        private readonly DateTimeInterval[] unitGroups = new DateTimeInterval[]
        {
            new (DateTimeUnit.Century, 1),
            new (DateTimeUnit.Decade, 20),
            new (DateTimeUnit.Decade, 10),
            new (DateTimeUnit.Decade, 5),
            new (DateTimeUnit.Decade, 2),
            new (DateTimeUnit.Decade, 1),
            new (DateTimeUnit.Year, 2),
            new (DateTimeUnit.Year, 1),
            new (DateTimeUnit.Month, 3),
            new (DateTimeUnit.Month, 1),
            new (DateTimeUnit.Month, 1/3f),
            new (DateTimeUnit.Month, 1/6f),
            new (DateTimeUnit.Day, 1),
            new (DateTimeUnit.Hour, 12),
            new (DateTimeUnit.Hour, 6),
            new (DateTimeUnit.Hour, 3),
            new (DateTimeUnit.Hour, 2),
            new (DateTimeUnit.Hour, 1),
            new (DateTimeUnit.Minute, 30),
            new (DateTimeUnit.Minute, 20),
            new (DateTimeUnit.Minute, 15),
            new (DateTimeUnit.Minute, 10),
            new (DateTimeUnit.Minute, 5),
            new (DateTimeUnit.Minute, 1),
            new (DateTimeUnit.Second, 30),
            new (DateTimeUnit.Second, 20),
            new (DateTimeUnit.Second, 15),
            new (DateTimeUnit.Second, 10),
            new (DateTimeUnit.Second, 5),
            new (DateTimeUnit.Second, 1),
        };

        [UxmlAttribute("start-time")]
        private string StartTimeUxml
        {
            get => StartValue.ToString("O");
            set => StartValue = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
        
        [UxmlAttribute("end-time")]
        private string EndTimeUxml
        {
            get => EndValue.ToString("O");
            set => EndValue = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
        
        [UxmlAttribute("current-time")]
        private string CurrentTimeUxml
        {
            get => CurrentValue.ToString("O");
            set => CurrentValue = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        public DateTime StartValue
        {
            get => startTime;
            set
            {
                startTime = value;
                duration = new TimeSpan(endTime.Ticks - startTime.Ticks);
                markElementsUpdateRequested = true;
                unitGroupsUpdateRequested = true;
            }
        }

        public DateTime EndValue
        {
            get => endTime;
            set
            {
                endTime = value;
                duration = new TimeSpan(endTime.Ticks - startTime.Ticks);
                markElementsUpdateRequested = true;
                unitGroupsUpdateRequested = true;
            }
        }
        
        public DateTime CurrentValue
        {
            get => currentTime;
            set
            {
                currentTime = value;
                preciseCurrentTime = value;
                displayedCurrentTime = value;
                rootTime = value;
                markElementsUpdateRequested = true;
            }
        }
        
        [UxmlAttribute]
        public DateTimeUnit UnitContext
        {
            get => unitContext;
            set
            {
                unitContext = value;
                markElementsUpdateRequested = true;
                unitGroupsUpdateRequested = true;
            }
        }
        
        [UxmlAttribute]
        public DateTimeUnit UnitPrecision
        {
            get => unitPrecision;
            set
            {
                unitPrecision = value;
                markElementsUpdateRequested = true;
                unitGroupsUpdateRequested = true;
            }
        }

        public DateTimeInterval SnapInterval
        {
            get => snapInterval;
            set
            {
                snapInterval = value;
                markElementsUpdateRequested = true;
                unitGroupsUpdateRequested = true;
            }
        }

        public Timeline()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            marksContainer = this.Q<VisualElement>("MarksContainer");
            labelsContainer = this.Q<VisualElement>("LabelsContainer");
            thumbElement = this.Q<VisualElement>("Thumb");
            thumbLabelElement = this.Q<DateTimeLabel>("ThumbLabel");
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            var viewport = this.Q<VisualElement>("MarksViewport");
            
            //Set up zooming
            viewport.RegisterCallback<WheelEvent>(OnMouseWheelScrolled);
            
            
            //SetupTimelineDragging();
            var timelineDragManipulator = new DragManipulator(4);
            timelineDragManipulator.DragStarted.AddListener(OnTimelineDragStarted);
            timelineDragManipulator.DraggingTotal.AddListener(OnTimelineDraggingTotal);
            timelineDragManipulator.DragEnded.AddListener(OnTimelineDragEnded);
            viewport.pickingMode = PickingMode.Position;
            viewport.AddManipulator(timelineDragManipulator);
            

            //set up scrubber
            var thumbDragManipulator = new DragManipulator(0);
            thumbDragManipulator.DragStarted.AddListener(OnThumbDragStarted);
            thumbDragManipulator.DraggingTotal.AddListener(OnThumbDraggingTotal);
            thumbDragManipulator.DragEnded.AddListener(OnThumbDragEnded);
            thumbElement.pickingMode = PickingMode.Position;
            thumbElement.AddManipulator(thumbDragManipulator);
            
            thumbDragUpdateItem = schedule.Execute(Update).Every(0);
        }

        private void Update()
        {
            UpdateSnap();
            UpdateThumbDrag();
            UpdateMarks();
        }
        
        private void OnMouseWheelScrolled(WheelEvent evt)
        {
            const float zoomSensitivity = 0.05f;
            var scrollDirection = evt.delta.y;
            var zoomMultiplier = Mathf.Pow(2.0f, zoomSensitivity * scrollDirection);

            var startSpan = (currentTime - startTime);
            var endSpan = (endTime - currentTime);
            
            startTime = currentTime - startSpan * zoomMultiplier;
            endTime = currentTime + endSpan * zoomMultiplier;
            duration = new TimeSpan(endTime.Ticks - startTime.Ticks);
            markElementsUpdateRequested = true;
            unitGroupsUpdateRequested = true;
        }
        
        private void OnTimelineDragStarted(Vector2 startPointerPosition)
        {
            timelineDragInitialStartTime = startTime;
            timelineDragInitialEndTime = endTime;
        }
        
        private void OnTimelineDraggingTotal(Vector2 deltaPointerPosition)
        {
            var offset = (long)Math.Round(ticksPerPixel * deltaPointerPosition.x);
            SetTimeRange(timelineDragInitialStartTime.AddTicks(-offset), timelineDragInitialEndTime.AddTicks(-offset));
            var currentTicks  = (long)Math.Clamp(displayedCurrentTime.Ticks, startTime.Ticks + duration.Ticks * thumbScrollMinPosition, startTime.Ticks + duration.Ticks * thumbScrollMaxPosition);
            preciseCurrentTime = new DateTime(currentTicks, displayedCurrentTime.Kind);
            currentTime = preciseCurrentTime.Round(snapInterval, rootTime);
            displayedCurrentTime = preciseCurrentTime;
            markElementsUpdateRequested = true;
        }
        
        private void OnTimelineDragEnded(Vector2 endPointerPosition)
        {
            StartSnap();
        }

        private void OnThumbDragStarted(Vector2 startPointerPosition)
        {
            isThumbDrag = true;
            
            thumbDragInitialThumbElementPosition = thumbElement.style.left.value.value / 100d;
            OnThumbDraggingTotal(Vector2.zero);
        }
        
        private void OnThumbDraggingTotal(Vector2 deltaPointerPosition)
        {
            var containerWidth = marksContainer.contentRect.width;
            var elementPositionOffset = (double)deltaPointerPosition.x / containerWidth;
            thumbPosition = thumbDragInitialThumbElementPosition + elementPositionOffset;

            thumbElementPosition = thumbDragInitialThumbElementPosition + elementPositionOffset;
        }

        
        private void UpdateThumbDrag()
        {
            if (!isThumbDrag)
                return;

            currentTime = startTime.Add(thumbPosition * duration);
            markElementsUpdateRequested = true;

            var minCurrentTime = PositionToTime(thumbScrollMinPosition, startTime, endTime);
            var maxCurrentTime = PositionToTime(thumbScrollMaxPosition, startTime, endTime);
            
            if (currentTime < minCurrentTime || currentTime > maxCurrentTime)
            {
                var boundTime = (currentTime < minCurrentTime) ? minCurrentTime : maxCurrentTime;
                var exceededPxWidth = Math.Clamp((currentTime - boundTime).Ticks / ticksPerPixel, -scrollAreaWidthPx, scrollAreaWidthPx);
                var scrollSpeed = exceededPxWidth / scrollAreaWidthPx;
                scrollSpeed = (currentTime - boundTime) / duration / scrollAreaWidth;
                var scrollOffset = scrollSpeed * maxScrollSpeed * Time.deltaTime * (endTime - startTime);
                startTime = startTime.Add(scrollOffset);
                endTime = endTime.Add(scrollOffset);
                thumbElementTimeOffset = currentTime - boundTime;
            }
            
            preciseCurrentTime = startTime.Add(Math.Clamp(thumbPosition, 0, 1) * duration);
            currentTime = preciseCurrentTime.Round(snapInterval, rootTime);
            displayedCurrentTime = preciseCurrentTime;
        }
        
        private void OnThumbDragEnded(Vector2 arg0)
        {
            isThumbDrag = false;

            currentTime = preciseCurrentTime.Round(snapInterval, rootTime);

            markElementsUpdateRequested = true;
            
            StartSnap();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!Mathf.Approximately(evt.oldRect.width, evt.newRect.width))
            {
                markElementsUpdateRequested = true;
                unitGroupsUpdateRequested = true;
            }
        }

        private void SetTimeRange(DateTime newStartTime, DateTime newEndTime)
        {
            var previousDuration = endTime - startTime;
            
            startTime = newStartTime;
            endTime = newEndTime;
            duration = new TimeSpan(endTime.Ticks - startTime.Ticks);

            markElementsUpdateRequested = true;
            if (endTime - startTime != previousDuration)
            {
                unitGroupsUpdateRequested = true;
            }

        }

        private double snapStartedRealTime;
        private DateTime snapStartedInitialStartTime;
        private DateTime snapStartedTargetStartTime;
        private DateTime snapStartedInitialEndTime;
        private DateTime snapStartedTargetEndTime;
        
        

        private void StartSnap()
        {
            isSnapping = true;
            snapStartedRealTime = Time.realtimeSinceStartupAsDouble;
            snapStartedThumbElementTimeOffset = thumbElementTimeOffset;
            snapStartedInitialStartTime = snapStartedTargetStartTime = startTime;
            snapStartedInitialEndTime = snapStartedTargetEndTime = endTime;
            
            var thumbScrollMinTime = PositionToTime(thumbScrollMinPosition, startTime, endTime);
            var thumbScrollMaxTime = PositionToTime(thumbScrollMaxPosition, startTime, endTime);
            if (currentTime < thumbScrollMinTime || currentTime > thumbScrollMaxTime)
            {
                var boundTime = (currentTime < thumbScrollMinTime) ? thumbScrollMinTime : thumbScrollMaxTime;
                var offset = currentTime - boundTime;
                snapStartedTargetStartTime = snapStartedInitialStartTime.Add(offset);
                snapStartedTargetEndTime = snapStartedInitialEndTime.Add(offset);
            }
        }

        private void UpdateSnap()
        {
            if (!isSnapping) return;

            var t = Math.Clamp((Time.realtimeSinceStartupAsDouble - snapStartedRealTime) / .2d, 0d, 1d);
            var eased = 1d - Math.Pow(1d - t, 3d);
            displayedCurrentTime = t >= 1d
                ? currentTime
                : preciseCurrentTime.Add((currentTime - preciseCurrentTime) * eased);

            thumbElementTimeOffset = t >= 1d
                ? TimeSpan.Zero
                : snapStartedThumbElementTimeOffset * (1-eased);

            startTime = t >= 1d
                ? snapStartedTargetStartTime
                : snapStartedInitialStartTime.Add((snapStartedTargetStartTime - snapStartedInitialStartTime) * eased);
            
            endTime = t >= 1d
                ? snapStartedTargetEndTime
                : snapStartedInitialEndTime.Add((snapStartedTargetEndTime - snapStartedInitialEndTime) * eased);
            
            
            var thumbMinTime = PositionToTime(thumbMinPosition, startTime, endTime);
            var thumbMaxTime = PositionToTime(thumbMaxPosition, startTime, endTime);
            if (displayedCurrentTime < thumbMinTime || displayedCurrentTime > thumbMaxTime)
            {
                var boundTime = (displayedCurrentTime < thumbMinTime) ? thumbMinTime : thumbMaxTime;
                var offset = displayedCurrentTime - boundTime;
                startTime = startTime.Add(offset);
                endTime = endTime.Add(offset);
            }
            
            markElementsUpdateRequested = true;
            UpdateThumb();
            
            if (t >= 1d)
                isSnapping = false;
        }

        private double TimeToPosition(DateTime time, DateTime start, DateTime end)
        {
            return (time - start).Ticks / (double)(end - start).Ticks;
        }

        private DateTime PositionToTime(double position, DateTime start, DateTime end)
        {
            return start.Add(position * (end - start));
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            StartSnap();
            markElementsUpdateRequested = true;
            unitGroupsUpdateRequested = true;
        }

        private void UpdateMarks()
        {
            if (!markElementsUpdateRequested) return;
                
            markElementsUpdateRequested = false;
            
            var width = marksContainer.resolvedStyle.width;

            if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0)
            {
                return;
            }

            if (endTime <= startTime)
            {
                return;
            }

            var maxMarkElements = (double)width / 10;
            var maxLabelElements = (double)width / 50;

            var start = startTime;
            var end = endTime;

            if (unitGroupsUpdateRequested)
            {
                unitGroupsUpdateRequested = false;
                marksInterval = DateTimeUtils.SelectDateTimeUnitGroup(unitGroups, unitContext, unitPrecision, start,
                    end, maxMarkElements);
                snapInterval = marksInterval;
                labelsInterval = DateTimeUtils.SelectDateTimeUnitGroup(unitGroups, unitContext, unitPrecision,
                    start, end, maxLabelElements);
            }

            ticksPerPixel = (duration.Ticks) / width;
            var paddingTicks = (long)Math.Round(ticksPerPixel * 50d);
            var markTimes = DateTimeUtils.GetDateTimesBetween(start.AddTicks(-paddingTicks),
                end.AddTicks(paddingTicks), marksInterval);
            var labelTimes = DateTimeUtils.GetDateTimesBetween(start.AddTicks(-paddingTicks),
                end.AddTicks(paddingTicks), labelsInterval);
            
            var primaryMarkUnit = (DateTimeUnit)Math.Clamp(Math.Max((int)DateTimeUnit.Century, (int)marksInterval.TimeUnit - 2), (int)unitContext, (int)unitPrecision);
            var primaryLabelUnit = (DateTimeUnit)Math.Clamp(Math.Max((int)DateTimeUnit.Century, (int)labelsInterval.TimeUnit - 1), (int)unitContext, (int)unitPrecision);

            thumbLabelElement.Context = unitContext;
            thumbLabelElement.Precision = primaryMarkUnit + 1;
            
            var markIndex = 0;
            foreach (var markTime in markTimes)
            {
                if (markIndex == markElements.Count)
                {
                    var newMark = new VisualElement();
                    newMark.pickingMode = PickingMode.Ignore;
                    newMark.AddToClassList("timeline__mark");
                    marksContainer.Add(newMark);
                    markElements.Add(newMark);
                }

                var markElement = markElements[markIndex];
                markIndex++;

                var position = (double)(markTime.Ticks - startTime.Ticks) / duration.Ticks;

                var left = Length.Percent((float)(position * 100));

                markElement.style.display = DisplayStyle.Flex;
                markElement.style.left = left;
                
                var isPrimary = markTime.RoundDown(primaryMarkUnit) == markTime;
                var isSecondary = !isPrimary && markTime.RoundDown(primaryMarkUnit + 1) == markTime;
                var isTertiary = !isPrimary && !isSecondary;
                
                markElement.EnableInClassList("timeline__mark--primary", isPrimary);
                markElement.EnableInClassList("timeline__mark--secondary", isSecondary);
                markElement.EnableInClassList("timeline__mark--tertiary", isTertiary);
            }

            for (var i = markIndex; i < markElements.Count; i++)
            {
                markElements[i].style.display = DisplayStyle.None;
            }

            var labelIndex = 0;
            foreach (var labelTime in labelTimes)
            {
                if (labelIndex == labelElements.Count)
                {
                    var newLabel = new DateTimeLabel();
                    newLabel.pickingMode = PickingMode.Ignore;
                    newLabel.AddToClassList("timeline__label");
                    labelsContainer.Add(newLabel);
                    labelElements.Add(newLabel);
                }

                var labelElement = labelElements[labelIndex];
                labelIndex++;

                var position = (double)(labelTime.Ticks - startTime.Ticks) / duration.Ticks;

                var left = Length.Percent((float)(position * 100));

                labelElement.style.display = DisplayStyle.Flex;
                labelElement.style.left = left;
                
                var isPrimary = labelTime.RoundDown(primaryLabelUnit) == labelTime;
                var isSecondary = !isPrimary && labelTime.RoundDown(primaryLabelUnit + 1) == labelTime;
                var isTertiary = !isPrimary && !isSecondary;

                var labelTimeUnit = primaryLabelUnit;
                if (isSecondary) labelTimeUnit = primaryLabelUnit + 1;
                if (isTertiary) labelTimeUnit = primaryLabelUnit + 2;

                labelElement.EnableInClassList("timeline__label--primary", isPrimary);
                
                labelElement.Context = labelTimeUnit;
                labelElement.Precision = labelTimeUnit;
                labelElement.Value = labelTime;
                labelElement.style.opacity = new StyleFloat(1f);
                
                //hide label element when it is behind thumb
                var pixelOffset = (labelTime - displayedCurrentTime).Ticks / ticksPerPixel;
                var opacity = Math.Clamp((float)(Math.Abs(pixelOffset) - 45f) / 5f, 0, 1);
                labelElement.style.opacity = new StyleFloat(opacity);
            }

            for (var i = labelIndex; i < labelElements.Count; i++)
            {
                labelElements[i].style.display = DisplayStyle.None;
            }
            
            UpdateThumb();
            UpdateTimeService();
        }

        private void UpdateTimeService()
        {
            if (currentTime != lastCurrentTime)
            {
                var sunTimeService = ServiceLocator.GetService<SunTime>();
                if (sunTimeService == null) return;
                sunTimeService.timeOfDayChanged.RemoveListener(OnSunTimeChanged);
                sunTimeService.SetTime(currentTime);
                sunTimeService.timeOfDayChanged.AddListener(OnSunTimeChanged);
                lastCurrentTime = currentTime;
            }
        }

        private void OnSunTimeChanged(DateTime arg0)
        {
            CurrentValue = arg0;
        }

        void UpdateThumb()
        {
            var elementPosition = Math.Clamp(TimeToPosition(displayedCurrentTime, startTime, endTime), 0, 1);
            thumbElement.style.left = Length.Percent((float)(elementPosition * 100));
            
            var thumbLabelPosition = Math.Clamp(TimeToPosition(displayedCurrentTime, startTime, endTime), 0, 1);
            thumbLabelElement.style.left = Length.Percent((float)(thumbLabelPosition * 100));
            thumbLabelElement.Value = currentTime;

        }
        
    }
}