using System;
using System.Collections.Generic;
using System.Globalization;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Timeline : VisualElement
    {
        
#region config
        
        private readonly int scrollAreaWidthPx = 50;
        private double scrollAreaWidth = .1f;

        private double thumbScrollMinPosition = .1d;
        private double thumbScrollMaxPosition = .9d;
        
        private double thumbMinPosition = 0d;
        private double thumbMaxPosition = 1d;
        private double maxScrollSpeed = 1d;
        
        private const float zoomSensitivity = 0.1f;
        private const float buttonZoomMultiplier = 5f;
        /// <summary>
        /// Whether grouped intervals need to be aligned to zero.
        /// When true: (15:22:38 + 7) seconds with a rounding interval of 10 seconds will be rounded to 15:30:38.
        /// When false: (15:22:38 + 7) seconds with a roudning interval of 10 seconds will be rounded to 15:32:38.
        /// </summary>
        private readonly bool alignGroupedIntervals = true; 

#endregion
       
        private readonly VisualElement marksContainer;
        private readonly List<VisualElement> markElements = new();
        
        private readonly VisualElement labelsContainer;
        private readonly List<TimelineLabel> labelElements = new();
        
        private readonly VisualElement thumbElement;
        private readonly DateTimeLabel thumbLabelElement;
        private readonly VisualElement currentTimeMarker;
        private readonly Button zoomInButton;
        private readonly Button zoomOutButton;
        
        private DateTime currentTime;
        private DateTime lastCurrentTime;
        private DateTime preciseCurrentTime;
        private DateTime displayedCurrentTime;
        private DateTime rootTime; //used for snapping
        private DateTime startTime;
        private DateTime endTime;
        private TimeSpan duration;
        private TimeSpan minDuration = new TimeSpan(0, 10, 0);
        private TimeSpan maxDuration = new TimeSpan(365 * 2000, 0, 0, 0);
        
        private double thumbPosition;
        
        private double ticksPerPixel;
        private double timePerPixel;

        private DateTimeUnit unitContext;
        private DateTimeUnit unitPrecision;
        private DateTimeInterval snapInterval = new DateTimeInterval(DateTimeUnit.Year, 1);

        private readonly List<DateTime> markTimes = new List<DateTime>(256);
        private readonly List<DateTime> labelTimes = new List<DateTime>(32);
        private DateTimeInterval marksInterval;
        private DateTimeInterval labelsInterval;

        private DateTime timelineDragInitialStartTime;
        private DateTime timelineDragInitialEndTime;
        private double timelineDragMinPosition;
        private double timelineDragMaxPosition;

        private bool isThumbDrag;
        private double thumbDragInitialThumbElementPosition;
        private DateTime thumbDragInitialStartTime;
        private DateTime thumbDragInitialEndTime;
        private readonly IVisualElementScheduledItem thumbDragUpdateItem;
        private TimeSpan thumbElementTimeOffset;

        private bool markElementsUpdateRequested = false;
        private bool unitGroupsUpdateRequested = false;

        private bool isSnapping;
        private TimeSpan snapStartedThumbElementTimeOffset;

        private readonly DateTimeInterval[] unitGroups = new DateTimeInterval[]
        {
            new (DateTimeUnit.Year, 100),
            new (DateTimeUnit.Year, 50),
            new (DateTimeUnit.Year, 20),
            new (DateTimeUnit.Year, 10),
            new (DateTimeUnit.Year, 5),
            new (DateTimeUnit.Year, 2),
            new (DateTimeUnit.Year, 1),
            new (DateTimeUnit.Month, 3),
            new (DateTimeUnit.Month, 1),
            new (DateTimeUnit.Month, 1/3d),
            new (DateTimeUnit.Month, 1/6d),
            new (DateTimeUnit.Day, 2),
            new (DateTimeUnit.Day, 1),
            new (DateTimeUnit.Hour, 6),
            new (DateTimeUnit.Hour, 3),
            new (DateTimeUnit.Hour, 2),
            new (DateTimeUnit.Hour, 1),
            new (DateTimeUnit.Minute, 30),
            new (DateTimeUnit.Minute, 20),
            new (DateTimeUnit.Minute, 15),
            new (DateTimeUnit.Minute, 10),
            new (DateTimeUnit.Minute, 5),
            new (DateTimeUnit.Minute, 2),
            new (DateTimeUnit.Minute, 1),
            new (DateTimeUnit.Second, 30),
            new (DateTimeUnit.Second, 20),
            new (DateTimeUnit.Second, 15),
            new (DateTimeUnit.Second, 10),
            new (DateTimeUnit.Second, 5),
            new (DateTimeUnit.Second, 2),
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

        private DateTime unpaddedStartValue;
        private DateTime unpaddedEndValue;
        
        private DateTime StartValue
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

        private DateTime EndValue
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
                AdjustTimeRange(value);
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
            currentTimeMarker = this.Q<VisualElement>("CurrentTimeMarker");
            zoomInButton =  this.Q<Button>("ZoomInButton");
            zoomOutButton = this.Q<Button>("ZoomOutButton");
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            zoomInButton.RegisterCallback<ClickEvent>(ZoomIn);
            zoomOutButton.RegisterCallback<ClickEvent>(ZoomOut);
            
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

        public void SetUnpaddedRange(DateTime min, DateTime max, DateTime current)
        {
            CurrentValue = current;
            unpaddedStartValue = min;
            unpaddedEndValue = max;
            SetRange(min, max);
        }
        
        public void SetRange(DateTime addedMin, DateTime addedMax)
        {
            var currentTime = CurrentValue;
            var newMin = Min(Min(unpaddedStartValue, addedMin), currentTime);
            var newMax = Max(Max(unpaddedEndValue, addedMax), currentTime);

            unpaddedStartValue = newMin;
            unpaddedEndValue = newMax;
            
            if (thumbScrollMaxPosition == thumbScrollMinPosition)
            {
                StartValue = newMin;
                EndValue = newMax;
                return;
            }
            
            // We will set min and max at the positions where the timeline starts scrolling.
            double selectedTicks = (newMax - newMin).Ticks;
            double fullTicks = selectedTicks / (thumbScrollMaxPosition - thumbScrollMinPosition);

            // Extend the new values by the fraction where the scrolling starts
            var realStart= newMin.AddTicks(-(long)Math.Round(fullTicks * thumbScrollMinPosition));
            var realEnd= newMax.AddTicks((long)Math.Round(fullTicks * (1d - thumbScrollMaxPosition)));

            StartValue = realStart;
            EndValue = realEnd;
        }
        
        static DateTime Min(DateTime a, DateTime b)
        {
            return a <= b ? a : b;
        }

        static DateTime Max(DateTime a, DateTime b)
        {
            return a >= b ? a : b;
        }
        
        private void Update()
        {
            UpdateSnap();
            UpdateThumbDrag();
            UpdateMarks();
        }

        private void ZoomIn(ClickEvent e)
        {
            var zoomMultiplier = Mathf.Pow(2.0f, zoomSensitivity * -buttonZoomMultiplier);
            Zoom(zoomMultiplier);
        }

        private void ZoomOut(ClickEvent e)
        {
            var zoomMultiplier = Mathf.Pow(2.0f, zoomSensitivity * buttonZoomMultiplier);
            Zoom(zoomMultiplier);
        }
        
        private void OnMouseWheelScrolled(WheelEvent evt)
        {
            var scrollDirection = evt.delta.y;
            var zoomMultiplier = Mathf.Pow(2.0f, zoomSensitivity * scrollDirection);

            Zoom(zoomMultiplier);
        }

        private void Zoom(float zoomMultiplier)
        {
            var startSpan = (currentTime - startTime);
            var endSpan = (endTime - currentTime);
            
            var newStartTime = currentTime.AddClamped(-startSpan * zoomMultiplier);
            var newEndTime = currentTime.AddClamped(endSpan * zoomMultiplier);

            var newDuration = newEndTime - newStartTime;
            
            if (newDuration.Ticks < minDuration.Ticks)
            {
                var diff = minDuration - newDuration;
                var startFraction = startSpan / (startSpan + endSpan);
                var diffStart = diff * startFraction;
                var diffEnd = diff * (1 - startFraction);
                newStartTime = newStartTime.AddClamped(-diffStart);
                newEndTime = newEndTime.AddClamped(diffEnd);
            }
            
            if (newDuration.Ticks > maxDuration.Ticks)
            {
                var diff = maxDuration - newDuration;
                var startFraction = startSpan / (startSpan + endSpan);
                var diffStart = diff * startFraction;
                var diffEnd = diff * (1 - startFraction);
                newStartTime = newStartTime.AddClamped(-diffStart);
                newEndTime = newEndTime.AddClamped(diffEnd);
            }
            
            startTime = newStartTime;
            endTime = newEndTime;
            duration = new TimeSpan(endTime.Ticks - startTime.Ticks);
            markElementsUpdateRequested = true;
            unitGroupsUpdateRequested = true;
        }

        private void OnTimelineDragStarted(Vector2 startPointerPosition)
        {
            timelineDragInitialStartTime = startTime;
            timelineDragInitialEndTime = endTime;
            
            var position = Math.Clamp(
                TimeToPosition(displayedCurrentTime, startTime, endTime),
                0d, 1d);

            timelineDragMinPosition = Math.Min(thumbScrollMinPosition, position);
            timelineDragMaxPosition = Math.Max(thumbScrollMaxPosition, position);
        }
        
        private void OnTimelineDraggingTotal(Vector2 deltaPointerPosition)
        {
            var offset = (long)Math.Round(ticksPerPixel * deltaPointerPosition.x);
            OffsetTimeRange(timelineDragInitialStartTime, timelineDragInitialEndTime, new TimeSpan(-offset));
            var currentTicks  = (long)Math.Clamp(displayedCurrentTime.Ticks, startTime.Ticks + duration.Ticks * timelineDragMinPosition, startTime.Ticks + duration.Ticks * timelineDragMaxPosition);
            preciseCurrentTime = new DateTime(currentTicks, displayedCurrentTime.Kind);
            currentTime = preciseCurrentTime.Round(snapInterval, alignGroupedIntervals, rootTime);
            displayedCurrentTime = preciseCurrentTime;
            markElementsUpdateRequested = true;
        }

        /// <summary>
        /// Adjust the startTime and entTime (if needed) so that currentTime is visible.
        /// </summary>
        /// <param name="time"></param>
        private void AdjustTimeRange(DateTime time)
        {
            var position = TimeToPosition(time, startTime, endTime);

            if (position >= thumbScrollMinPosition && position <= thumbScrollMinPosition)
            {
                return;
            }

            var positionOffset = 0d;
            
            if (position < thumbScrollMinPosition)
            {
                positionOffset = position - thumbScrollMinPosition;
            } else if (position > thumbScrollMaxPosition)
            {
                positionOffset = position - thumbScrollMaxPosition;
            }
            
            OffsetTimeRange(startTime, endTime, positionOffset * (endTime - startTime));

        }
        
        private void OnTimelineDragEnded(Vector2 endPointerPosition)
        {
            rootTime = currentTime;
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
        }

        
        private void UpdateThumbDrag()
        {
            if (!isThumbDrag)
                return;

            currentTime = startTime.AddClamped(thumbPosition * duration);
            


            var minCurrentTime = PositionToTime(thumbScrollMinPosition, startTime, endTime);
            var maxCurrentTime = PositionToTime(thumbScrollMaxPosition, startTime, endTime);
            
            if (currentTime < minCurrentTime || currentTime > maxCurrentTime)
            {
                var boundTime = (currentTime < minCurrentTime) ? minCurrentTime : maxCurrentTime;
                var exceededPxWidth = Math.Clamp((currentTime - boundTime).Ticks / ticksPerPixel, -scrollAreaWidthPx, scrollAreaWidthPx);
                var scrollSpeed = exceededPxWidth / scrollAreaWidthPx;
                scrollSpeed = (currentTime - boundTime) / duration / scrollAreaWidth;
                var scrollOffset = scrollSpeed * maxScrollSpeed * Time.deltaTime * (endTime - startTime);
  
                OffsetTimeRange(startTime, endTime, scrollOffset);
                
                duration = endTime - startTime;
                thumbElementTimeOffset = currentTime - boundTime;

            }
            markElementsUpdateRequested = true;
            
            
            preciseCurrentTime = startTime.AddClamped(Math.Clamp(thumbPosition, 0, 1) * duration);
            currentTime = preciseCurrentTime.Round(snapInterval, alignGroupedIntervals, rootTime);
            displayedCurrentTime = preciseCurrentTime;
        }
        
        
        private void OnThumbDragEnded(Vector2 arg0)
        {
            isThumbDrag = false;

            currentTime = preciseCurrentTime.Round(snapInterval, alignGroupedIntervals, rootTime);
            rootTime = currentTime;

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

        private void OffsetTimeRange(DateTime start, DateTime end, TimeSpan timespan)
        {
            var clampedScrollOffset = TimeSpan.FromTicks(Math.Clamp(timespan.Ticks, DateTime.MinValue.Ticks - start.Ticks, DateTime.MaxValue.Ticks - end.Ticks));
            SetTimeRange(start.AddClamped(clampedScrollOffset), end.AddClamped(clampedScrollOffset));
            markElementsUpdateRequested = true;
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
  
                var offsetTicks = Math.Clamp((currentTime - boundTime).Ticks, 
                    DateTime.MinValue.Ticks - snapStartedInitialStartTime.Ticks, 
                    DateTime.MaxValue.Ticks - snapStartedInitialEndTime.Ticks);

                snapStartedTargetStartTime = snapStartedInitialStartTime.AddTicks(offsetTicks);
                snapStartedTargetEndTime = snapStartedInitialEndTime.AddTicks(offsetTicks);
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
                OffsetTimeRange(startTime, endTime, offset);
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
            return start.AddClamped(position * (end - start));
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (Application.isPlaying)
            {
                ProjectData.Current.OnCurrentDateTimeChanged.AddListener(HandleProjectTimeChanged);
            }
            
            StartSnap();
            markElementsUpdateRequested = true;
            unitGroupsUpdateRequested = true;
        }

        public void UpdateTimelineVisibility(bool visible)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        
            if (visible)
            {
                markElementsUpdateRequested = true;
                unitGroupsUpdateRequested = true;
            } 
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (Application.isPlaying)
            {
                ProjectData.Current.OnCurrentDateTimeChanged.RemoveListener(HandleProjectTimeChanged);
            }
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
            var maxLabelElements = (double)width / 64;

            if (unitGroupsUpdateRequested)
            {
                unitGroupsUpdateRequested = false;
                marksInterval = DateTimeUtils.SelectDateTimeUnitGroup(unitGroups, unitContext, unitPrecision, startTime,
                    endTime, maxMarkElements);
                snapInterval = marksInterval;
                labelsInterval = DateTimeUtils.SelectDateTimeUnitGroup(unitGroups, unitContext, unitPrecision,
                    startTime, endTime, maxLabelElements);
            }

            ticksPerPixel = (duration.Ticks) / width;
            var paddingTicks = (long)Math.Round(ticksPerPixel * 50d);
            var paddedStartTime = startTime.AddTicksClamped(-paddingTicks);
            var paddedEndTime = endTime.AddTicksClamped(paddingTicks);
            DateTimeUtils.GetDateTimesBetweenNonAlloc(markTimes, paddedStartTime, paddedEndTime, marksInterval, alignGroupedIntervals);
            DateTimeUtils.GetDateTimesBetweenNonAlloc(labelTimes, paddedStartTime, paddedEndTime, labelsInterval, alignGroupedIntervals);

            var primaryMarkUnit = (DateTimeUnit)Math.Clamp(Math.Max((int)DateTimeUnit.Year, (int)(labelsInterval.Amount >= 1 ? labelsInterval.TimeUnit : labelsInterval.TimeUnit + 1)), (int)unitContext, (int)unitPrecision);
            var primaryLabelUnit = (DateTimeUnit)Math.Clamp(Math.Max((int)DateTimeUnit.Year, (int)(labelsInterval.Amount >= 1 ? labelsInterval.TimeUnit : labelsInterval.TimeUnit + 1)), (int)unitContext, (int)unitPrecision);

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
                    var newLabel = new TimelineLabel();
                    newLabel.pickingMode = PickingMode.Ignore;
                    labelsContainer.Add(newLabel);
                    labelElements.Add(newLabel);
                }

                var labelElement = labelElements[labelIndex];
                labelIndex++;

                var position = (double)(labelTime.Ticks - startTime.Ticks) / duration.Ticks;

                var left = Length.Percent((float)(position * 100));

                labelElement.style.display = DisplayStyle.Flex;
                labelElement.style.left = left;
                
                var isPrimary = labelTime.RoundDown((DateTimeUnit)Math.Max(0, (int)primaryLabelUnit-1)) == labelTime;
                var isSecondary = !isPrimary && labelTime.RoundDown(
                    (DateTimeUnit)Math.Min(Enum.GetValues(typeof(DateTimeUnit)).Length - 1, (int)primaryLabelUnit + 1)) == labelTime;
                var isTertiary = !isPrimary && !isSecondary;

                var labelTimeUnit = primaryLabelUnit;

                labelElement.UnitContext = (DateTimeUnit)Math.Max(0, (int)labelTimeUnit-1);
                labelElement.UnitPrecision = labelTimeUnit;
                labelElement.EnableSubLabel = isPrimary;
                labelElement.Time = labelTime;

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
            if (currentTime == lastCurrentTime) return;
            var projectData = ProjectData.Current;
            if (projectData == null) return;
            projectData.UseCurrentTime = false;
            projectData.CurrentDateTime = currentTime;
            lastCurrentTime = currentTime;
        }

        private void HandleProjectTimeChanged(DateTime time)
        {
            lastCurrentTime = time;
            CurrentValue = time;
        }

        void UpdateThumb()
        {
            var elementPosition = Math.Clamp(TimeToPosition(displayedCurrentTime, startTime, endTime), 0, 1);
            thumbElement.style.left = Length.Percent((float)(elementPosition * 100));
            
            var thumbLabelPosition = Math.Clamp(TimeToPosition(displayedCurrentTime, startTime, endTime), 0, 1);
            thumbLabelElement.style.left = Length.Percent((float)(thumbLabelPosition * 100));
            thumbLabelElement.Value = currentTime;

            var markerPosition = Math.Clamp(TimeToPosition(currentTime, startTime, endTime), 0, 1);
            currentTimeMarker.style.left = Length.Percent((float)(markerPosition * 100));
        }
        
    }
}