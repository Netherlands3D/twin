using System;
using System.Collections.Generic;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TimelineBar : VisualElement
    {
        public event Action<int, int> TimeChanged;
        
        private const int RenderPadding = 50;
        private const int ThumbScrollAreaSize = 100; 
        private const int ThumbScrollSpeed = 100;

        private double thumbMaxScrollSpeed = .25d;
        private double thumbCurrentScrollSpeed = 0d;
        private int scrollAreaWidth = 50;
        private double thumbMinPosition = .45d;
        private double thumbMaxPosition = .55d;
       
        private readonly VisualElement marksContainer;
        private readonly List<VisualElement> markElements = new();
        
        private readonly VisualElement labelsContainer;
        private readonly List<DateTimeLabel> labelElements = new();
        
        private readonly VisualElement thumbElement;

        private DateTime currentValue;
        private DateTime roundedCurrentValue;
        private DateTime startValue;
        private DateTime endValue;
        private TimeSpan duration;
        private double ticksPerPixel;
        
        private DateTimeUnit unitContext;
        private DateTimeUnit unitPrecision;

        private DateTimeInterval marksInterval;
        private DateTimeInterval labelsInterval;

        private DateTime timelineDragInitialStartTime;
        private DateTime timelineDragInitialEndTime;

        private bool isThumbDrag;
        private DateTime thumbDragInitialCurrentTime;
        private double thumbDragInitialThumbElementPosition;
        private DateTime thumbDragInitialStartTime;
        private DateTime thumbDragInitialEndTime;
        private double thumbElementPosition;
        private readonly IVisualElementScheduledItem thumbDragUpdateItem;

        private bool markElementsUpdateRequested = false;
        private bool unitGroupsUpdateRequested = false;
        
        private readonly DateTimeInterval[] unitGroups = new DateTimeInterval[]
        {
            new (DateTimeUnit.Year, 1000),
            new (DateTimeUnit.Year, 500),
            new (DateTimeUnit.Year, 200),
            new (DateTimeUnit.Year, 100),
            new (DateTimeUnit.Year, 50),
            new (DateTimeUnit.Year, 20),
            new (DateTimeUnit.Year, 10),
            new (DateTimeUnit.Year, 5),
            new (DateTimeUnit.Year, 2),
            new (DateTimeUnit.Year, 1),
            new (DateTimeUnit.Month, 6),
            new (DateTimeUnit.Month, 3),
            new (DateTimeUnit.Month, 2),
            new (DateTimeUnit.Month, 1),
            new (DateTimeUnit.Month, 1/2f),
            new (DateTimeUnit.Month, 1/4f),
            new (DateTimeUnit.Month, 1/8f),
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

        public DateTime StartValue
        {
            get => startValue;
            set
            {
                startValue = value;
                duration = new TimeSpan(endValue.Ticks - startValue.Ticks);
                markElementsUpdateRequested = true;
                unitGroupsUpdateRequested = true;
            }
        }

        public DateTime EndValue
        {
            get => endValue;
            set
            {
                endValue = value;
                duration = new TimeSpan(endValue.Ticks - startValue.Ticks);
                markElementsUpdateRequested = true;
                unitGroupsUpdateRequested = true;
            }
        }
        
        public DateTime CurrentValue
        {
            get => currentValue;
            set
            {
                currentValue = value;
                markElementsUpdateRequested = true;
            }
        }
        
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

        public TimelineBar()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            marksContainer = this.Q<VisualElement>("MarksContainer");
            labelsContainer = this.Q<VisualElement>("LabelsContainer");
            thumbElement = this.Q<VisualElement>("Thumb");
            
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
            UpdateThumbDrag();
            UpdateMarks();
        }
        
        private void OnMouseWheelScrolled(WheelEvent evt)
        {
            //todo: zoom in and out
        }
        
        private void OnTimelineDragStarted(Vector2 startPointerPosition)
        {
            timelineDragInitialStartTime = startValue;
            timelineDragInitialEndTime = endValue;
        }
        
        private void OnTimelineDraggingTotal(Vector2 deltaPointerPosition)
        {
            var offset = (long)Math.Round(ticksPerPixel * deltaPointerPosition.x);
            SetTimeRange(timelineDragInitialStartTime.AddTicks(-offset), timelineDragInitialEndTime.AddTicks(-offset));
            var currentTicks  = (long)Math.Clamp(currentValue.Ticks, startValue.Ticks + duration.Ticks * thumbMinPosition, startValue.Ticks + duration.Ticks * thumbMaxPosition);
            currentValue = new DateTime(currentTicks, currentValue.Kind);
        }
        
        private void OnTimelineDragEnded(Vector2 endPointerPosition)
        {
        }

        private void OnThumbDragStarted(Vector2 startPointerPosition)
        {
            isThumbDrag = true;
            thumbCurrentScrollSpeed = 0;
            thumbDragInitialCurrentTime = currentValue;
            thumbDragInitialThumbElementPosition = thumbElement.style.left.value.value / 100d;
            OnThumbDraggingTotal(Vector2.zero);
        }
        
        private void OnThumbDraggingTotal(Vector2 deltaPointerPosition)
        {
            var containerWidth = marksContainer.contentRect.width;
            var elementPositionOffset = (double)deltaPointerPosition.x / containerWidth;
            thumbElementPosition = thumbDragInitialThumbElementPosition + elementPositionOffset;
        }

        
        private void UpdateThumbDrag()
        {

            if (!isThumbDrag)
                return;
            
            if (thumbElementPosition < thumbMinPosition || thumbElementPosition > thumbMaxPosition)
            {
                var containerWidth = marksContainer.resolvedStyle.width;
                var scrollAreaSize = scrollAreaWidth / containerWidth;
                var boundPosition = (thumbElementPosition < thumbMinPosition) ? thumbMinPosition : thumbMaxPosition;
                var exceedBoundsPosition = Math.Clamp(thumbElementPosition - boundPosition, -scrollAreaSize, scrollAreaSize);
                thumbCurrentScrollSpeed = exceedBoundsPosition / scrollAreaSize;
                var ticksOffset = (long)Math.Round(thumbCurrentScrollSpeed * thumbMaxScrollSpeed * Time.deltaTime * (endValue.Ticks - startValue.Ticks));
                SetTimeRange(startValue.AddTicks(ticksOffset), endValue.AddTicks(ticksOffset));
            }
            
            var clampedThumbElementPosition = Math.Clamp(thumbElementPosition, thumbMinPosition, thumbMaxPosition);
            CurrentValue = startValue.Add(clampedThumbElementPosition * duration);
            CurrentValue = new DateTime(CurrentValue.Year, CurrentValue.Month, CurrentValue.Day, thumbDragInitialCurrentTime.Hour, thumbDragInitialCurrentTime.Minute, thumbDragInitialCurrentTime.Second, thumbDragInitialCurrentTime.Millisecond);
        }

        private void OnThumbDragEnded(Vector2 arg0)
        {
            isThumbDrag = false;
            thumbCurrentScrollSpeed = 0;
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
            var previousDuration = endValue - startValue;
            
            startValue = newStartTime;
            endValue = newEndTime;
            duration = new TimeSpan(endValue.Ticks - startValue.Ticks);

            markElementsUpdateRequested = true;
            if (endValue - startValue != previousDuration)
            {
                unitGroupsUpdateRequested = true;
            }

        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
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

            if (endValue <= startValue)
            {
                return;
            }

            var maxMarkElements = (double)width / 10;
            var maxLabelElements = (double)width / 50;

            var start = startValue;
            var end = endValue;

            if (unitGroupsUpdateRequested)
            {
                unitGroupsUpdateRequested = false;
                marksInterval = DateTimeUtils.SelectDateTimeUnitGroup(unitGroups, unitContext, unitPrecision, start,
                    end, maxMarkElements);
                labelsInterval = DateTimeUtils.SelectDateTimeUnitGroup(unitGroups, unitContext, unitPrecision,
                    start, end, maxLabelElements);
            }

            ticksPerPixel = (duration.Ticks) / width;
            var paddingTicks = (long)Math.Round(ticksPerPixel * 50d);
            var markTimes = DateTimeUtils.GetDateTimesBetween(start.AddTicks(-paddingTicks),
                end.AddTicks(paddingTicks), marksInterval);
            var labelTimes = DateTimeUtils.GetDateTimesBetween(start.AddTicks(-paddingTicks),
                end.AddTicks(paddingTicks), labelsInterval);

            var markIndex = 0;
            foreach (var markTime in markTimes)
            {
                if (markIndex == markElements.Count)
                {
                    var newMark = new VisualElement();
                    newMark.pickingMode = PickingMode.Ignore;
                    newMark.AddToClassList("timeline-bar__mark");
                    marksContainer.Add(newMark);
                    markElements.Add(newMark);
                }

                var markElement = markElements[markIndex];
                markIndex++;

                var position = (double)(markTime.Ticks - startValue.Ticks) / duration.Ticks;

                var left = Length.Percent((float)(position * 100));

                markElement.style.display = DisplayStyle.Flex;
                markElement.style.left = left;
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
                    newLabel.AddToClassList("timeline-bar__label");
                    labelsContainer.Add(newLabel);
                    labelElements.Add(newLabel);
                }

                var labelElement = labelElements[labelIndex];
                labelIndex++;

                var position = (double)(labelTime.Ticks - startValue.Ticks) / duration.Ticks;

                var left = Length.Percent((float)(position * 100));

                labelElement.style.display = DisplayStyle.Flex;
                labelElement.style.left = left;
                labelElement.Value = labelTime;
            }

            for (var i = labelIndex; i < labelElements.Count; i++)
            {
                labelElements[i].style.display = DisplayStyle.None;
            }
            
            
            //update thumb

            UpdateThumb();
            UpdateTimeService();

        }

        private void UpdateTimeService()
        {
            var sunTimeService = ServiceLocator.GetService<SunTime>();
            if (sunTimeService == null) return;
            sunTimeService.timeOfDayChanged.RemoveListener(OnSunTimeChanged);
            sunTimeService.SetTime(currentValue);
            sunTimeService.timeOfDayChanged.AddListener(OnSunTimeChanged);
        }

        private void OnSunTimeChanged(DateTime arg0)
        {
            CurrentValue = arg0;
        }

        void UpdateThumb()
        {
            var position = (double)(currentValue.Ticks - startValue.Ticks) / duration.Ticks;
            var left = Length.Percent((float)(position * 100));
            thumbElement.style.left = left;
        }
        
    }
}