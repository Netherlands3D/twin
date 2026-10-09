using System;
using System.Collections.Generic;
using log4net.DateFormatter;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TimelineBar : VisualElement
    {
        private readonly VisualElement marksContainer;
        private readonly List<VisualElement> markElements = new();
        
        private readonly VisualElement labelsContainer;
        private readonly List<DateTimeLabel> labelElements = new();

        private readonly VisualElement scrubberElement;
        
        
        private DateTime currentValue;
        private DateTime startValue;
        private DateTime endValue;
        private DateTimeUnit unitContext;
        private DateTimeUnit unitPrecision;

        private DateTimeInterval marksInterval;
        private DateTimeInterval labelsInterval;

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
            scrubberElement = this.Q<VisualElement>("Scrubber");
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            SetupTimelineDragging();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!Mathf.Approximately(evt.oldRect.width, evt.newRect.width))
            {
                RequestUpdateMarkElements(true);
            }
        }

        private void SetupTimelineDragging()
        {
            var viewport = this.Q<VisualElement>("MarksViewport");
            viewport.pickingMode = PickingMode.Position;

            var pointerId = -1;
            var initialX = 0f;
            var width = 0f;
            var initialStart = default(DateTime);
            var initialEnd = default(DateTime);

            viewport.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || pointerId != -1)
                    return;

                width = viewport.contentRect.width;
                if (width <= 0)
                    return;

                initialX = evt.localPosition.x;
                initialStart = startValue;
                initialEnd = endValue;
                pointerId = evt.pointerId;

                viewport.CapturePointer(pointerId);
                evt.StopPropagation();
            });

            viewport.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (evt.pointerId != pointerId ||
                    !viewport.HasPointerCapture(pointerId))
                    return;

                var deltaX = evt.localPosition.x - initialX;
                var duration = (initialEnd - initialStart).TotalSeconds;
                var offset = -(deltaX / width) * duration;

                SetTimeRange(
                    initialStart.AddSeconds(offset),
                    initialEnd.AddSeconds(offset));

                evt.StopPropagation();
            });

            viewport.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (evt.pointerId != pointerId || evt.button != 0)
                    return;

                viewport.ReleasePointer(pointerId);
                pointerId = -1;
                evt.StopPropagation();
            });

            viewport.RegisterCallback<PointerCaptureOutEvent>(evt =>
            {
                if (evt.pointerId == pointerId)
                    pointerId = -1;
            });
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
            
            schedule.Execute(() =>
            {
                marksUpdateQueued = false;
                UpdateMarks();
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
            
                var duration = endValue.Ticks - startValue.Ticks;

                var markTimes = new List<DateTime>();
                DateTimeUtils.GetDateTimesBetweenNonAlloc(markTimes, start, end, marksInterval);
                
                var labelTimes = new List<DateTime>();
                DateTimeUtils.GetDateTimesBetweenNonAlloc(labelTimes, start, end, labelsInterval);

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

                    var position =
                        (double)(markTime.Ticks - startValue.Ticks) / duration;

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

                    var position =
                        (double)(labelTime.Ticks - startValue.Ticks) / duration;

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
            
            
        }
        
    }
}