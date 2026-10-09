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
        private readonly VisualElement marksContainer;
        private readonly List<VisualElement> markElements = new();
        private DateTime currentValue;
        private DateTime startValue;
        private DateTime endValue;

        private DateTimeUnitGroup marksUnitGroup;

        private bool marksUpdateQueued = false;
        private bool unitGroupsUpdateQueued = false;
        
        private readonly DateTimeUnitGroup[] unitGroups = new DateTimeUnitGroup[]
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

        public TimelineBar()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            marksContainer = this.Q<VisualElement>("MarksContainer");
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            marksContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            SetupDragging();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!Mathf.Approximately(evt.oldRect.width, evt.newRect.width))
            {
                RequestUpdateMarkElements(true);
            }
        }

        private void SetupDragging()
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
                
                var maxNumMarks = (double)width / 4;

                var start = startValue;
                var end = endValue;
                
                if (unitGroupsUpdateQueued)
                {
                    unitGroupsUpdateQueued = false;
                    marksUnitGroup = DateTimeUtils.SelectDateTimeUnitGroup(unitGroups, start, end, maxNumMarks);
                }
            
                var duration = endValue.Ticks - startValue.Ticks;
                
                var markDateTimes =
                    marksUnitGroup.TimeUnit.GetDateTimesBetween(start, end, marksUnitGroup.TimeUnitFactor);

                var markIndex = 0;
                foreach (var markDateTime in markDateTimes)
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
                        (double)(markDateTime.Ticks - startValue.Ticks) / duration;

                    var left = Length.Percent((float)(position * 100));

                    markElement.style.display = DisplayStyle.Flex;
                    markElement.style.left = left;
                }

                for (var i = markIndex; i < markElements.Count; i++)
                {
                    markElements[i].style.display = DisplayStyle.None;
                }
            
            }
        }
        
    }
}