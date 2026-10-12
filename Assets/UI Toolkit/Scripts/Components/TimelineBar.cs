using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TimelineBar : VisualElement
    {
        private const int RenderPadding = 50;
        private const int ThumbScrollAreaSize = 100; 
        private const int ThumbScrollSpeed = 100;
       
        private readonly VisualElement marksContainer;
        private readonly List<VisualElement> markElements = new();
        
        private readonly VisualElement labelsContainer;
        private readonly List<DateTimeLabel> labelElements = new();
        
        private readonly VisualElement thumbElement;

        private DateTime currentValue;
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

        private DateTime thumbDragInitialCurrentTime;
        private double thumbDragInitialThumbElementPosition;
        private DateTime thumbDragInitialStartTime;
        private DateTime thumbDragInitialEndTime;
        private double thumbElementPosition;
        private readonly IVisualElementScheduledItem thumbDragUpdateItem;

        private bool marksUpdateQueued = false;
        private bool unitGroupsUpdateQueued = false;
        
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
                RequestUpdateMarkElements(true);
            }
        }

        public DateTime EndValue
        {
            get => endValue;
            set
            {
                endValue = value;
                RequestUpdateMarkElements(true);
            }
        }
        
        public DateTime CurrentValue
        {
            get => currentValue;
            set
            {
                currentValue = value;
                RequestUpdateMarkElements(false);
            }
        }
        
        public DateTimeUnit UnitContext
        {
            get => unitContext;
            set
            {
                unitContext = value;
                RequestUpdateMarkElements(true);
            }
        }
        
        public DateTimeUnit UnitPrecision
        {
            get => unitPrecision;
            set
            {
                unitPrecision = value;
                RequestUpdateMarkElements(true);
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
            
            //SetupTimelineDragging();
            var viewport = this.Q<VisualElement>("MarksViewport");
            var timelineDragManipulator = new DragManipulator(4);
            timelineDragManipulator.DragStarted.AddListener(OnTimelineDragStarted);
            timelineDragManipulator.DraggingTotal.AddListener(OnTimelineDraggingTotal);
            timelineDragManipulator.DragEnded.AddListener(OnTimelineDragEnded);
            viewport.pickingMode = PickingMode.Position;
            viewport.AddManipulator(timelineDragManipulator);
            
            
            //set up scrubber
            var thumbDragManipulator = new DragManipulator(4);
            thumbDragManipulator.DragStarted.AddListener(OnThumbDragStarted);
            thumbDragManipulator.DraggingTotal.AddListener(OnThumbDraggingTotal);
            thumbDragManipulator.DragEnded.AddListener(OnThumbDragEnded);
            thumbElement.pickingMode = PickingMode.Position;
            thumbElement.AddManipulator(thumbDragManipulator);
            thumbDragUpdateItem = schedule.Execute(UpdateThumbDrag).Every(0);
            thumbDragUpdateItem.Pause();

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
        }
        
        private void OnTimelineDragEnded(Vector2 endPointerPosition)
        {
        }

        private void OnThumbDragStarted(Vector2 startPointerPosition)
        {
            thumbDragInitialCurrentTime = currentValue;
            thumbDragInitialThumbElementPosition = thumbElement.style.left.value.value / 100d;
            thumbDragUpdateItem.Resume();
        }
        
        private void OnThumbDraggingTotal(Vector2 deltaPointerPosition)
        {
            var containerWidth = marksContainer.contentRect.width;
            var elementPositionOffset = (double)deltaPointerPosition.x / containerWidth;
            thumbElementPosition = thumbDragInitialThumbElementPosition + elementPositionOffset;
        }
        
        private void UpdateThumbDrag()
        {
            if (thumbElementPosition < 0)
            {
                SetTimeRange(startValue.AddMonths(-1), endValue.AddMonths(-1));
            }
            else if (thumbElementPosition > 1)
            {
                SetTimeRange(startValue.AddMonths(1), endValue.AddMonths(1));
            }
            
            var clampedThumbElementPosition = Math.Clamp(thumbElementPosition, 0, 1);
            CurrentValue = startValue.Add(clampedThumbElementPosition * duration);
        }
        
        private void OnThumbDragEnded(Vector2 arg0)
        {
            thumbDragUpdateItem.Pause();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!Mathf.Approximately(evt.oldRect.width, evt.newRect.width))
            {
                RequestUpdateMarkElements(true);
            }
        }

        private void SetTimeRange(DateTime newStartTime, DateTime newEndTime)
        {
            var previousDuration = endValue - startValue;
            
            startValue = newStartTime;
            endValue = newEndTime;

            RequestUpdateMarkElements(endValue - startValue != previousDuration);

        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RequestUpdateMarkElements(true);
        }

        private void RequestUpdateMarkElements(bool requestUnitGroupUpdate)
        {
            unitGroupsUpdateQueued = requestUnitGroupUpdate || unitGroupsUpdateQueued;
            
            if (marksUpdateQueued)
            {
                return;
            }

            marksUpdateQueued = true;
            
            var sdfdsf = schedule.Execute(() =>
            {
                marksUpdateQueued = false;
                UpdateMarks();
                UpdateThumb();
            });

            return;
            
            void UpdateMarks()
            {
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
                
                if (unitGroupsUpdateQueued)
                {
                    unitGroupsUpdateQueued = false;
                    marksInterval = DateTimeUtils.SelectDateTimeUnitGroup(unitGroups, unitContext, unitPrecision, start, end, maxMarkElements);
                    labelsInterval = DateTimeUtils.SelectDateTimeUnitGroup(unitGroups, unitContext, unitPrecision, start, end, maxLabelElements);
                }
            
                duration = new TimeSpan(endValue.Ticks - startValue.Ticks);
                ticksPerPixel = (duration.Ticks) / width;
                var paddingTicks = (long)Math.Round(ticksPerPixel * 50d);
                var markTimes = DateTimeUtils.GetDateTimesBetween(start.AddTicks(-paddingTicks), end.AddTicks(paddingTicks), marksInterval);
                var labelTimes = DateTimeUtils.GetDateTimesBetween(start.AddTicks(-paddingTicks), end.AddTicks(paddingTicks), labelsInterval);

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
            }

            void UpdateThumb()
            {
                var position = (double)(currentValue.Ticks - startValue.Ticks) / duration.Ticks;
                var left = Length.Percent((float)(position * 100));
                thumbElement.style.left = left;
            }
        }
        
    }
}