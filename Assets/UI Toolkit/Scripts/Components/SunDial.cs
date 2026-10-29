using System;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class SunDial : VisualElement
    {
        private VisualElement dialFace;
        private VisualElement orbitContainer;
        private DragManipulator dragManipulator;

        private Vector2 pointerPositionWorldSpace;
        private Vector2 previousPointerPosition;
        private float angle;

        public event Action<int, int> TimeChanged;

        private VisualElement DialFace => dialFace ??= this.Q<VisualElement>("DialFace");
        private VisualElement OrbitContainer => orbitContainer ??= this.Q<VisualElement>("OrbitContainer");
        private DragManipulator DragManipulator => dragManipulator ??= new DragManipulator(0);

        private string draggingTooltip;
        private bool isDragging;
        private bool isOverDial;

        public SunDial()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            DialFace.AddManipulator(DragManipulator);
            DialFace.RegisterCallback<PointerEnterEvent>(OnDialPointerEnter);
            DialFace.RegisterCallback<PointerLeaveEvent>(OnDialPointerLeave);
            
            DragManipulator.DragStarted.AddListener(OnDragStarted);
            DragManipulator.Dragging.AddListener(OnDragging);
            DragManipulator.DragEnded.AddListener(OnDragEnded);
            
            draggingTooltip = OrbitContainer.tooltip;
        }
        private void OnDialPointerEnter(PointerEnterEvent evt)
        {
            PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRAB);
            isOverDial = true;
        }

        private void OnDialPointerLeave(PointerLeaveEvent evt)
        {
            if(!isDragging) 
                PointerStyle.CancelCursorChange(this);
            isOverDial = false;
        }

        public void SetTimeWithoutNotify(int hour, int minute)
        {
            var clampedHour = Mathf.Clamp(hour, 0, 23);
            var clampedMinute = Mathf.Clamp(minute, 0, 59);
            var convertedAngle = clampedHour * 15f + clampedMinute * 0.25f;
            SetAngleWithoutNotify(convertedAngle);
        }

        public void SetAngleWithoutNotify(float newAngle)
        {
            angle = newAngle;
            OrbitContainer.style.rotate = new StyleRotate(new Rotate(angle));
        }

        private void OnDragStarted(Vector2 startPosition)
        {
            pointerPositionWorldSpace = startPosition;
            previousPointerPosition = this.WorldToLocal(pointerPositionWorldSpace);
            PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRABBING);
            OrbitContainer.tooltip = "";
            isDragging = true;
        }

        private void OnDragEnded(Vector2 endPosition)
        {
            OrbitContainer.tooltip = draggingTooltip;
            if(isOverDial)
                PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRAB);
            else
                PointerStyle.CancelCursorChange(this);
            isDragging = false;
        }

        private void OnDragging(Vector2 delta)
        {
            pointerPositionWorldSpace += delta;
            var currentPosition = this.WorldToLocal(pointerPositionWorldSpace);

            var center = new Vector2(
                OrbitContainer.layout.x + OrbitContainer.layout.width * 0.5f,
                OrbitContainer.layout.y + OrbitContainer.layout.height * 0.5f
            );

            var previous = previousPointerPosition - center;
            var current = currentPosition - center;

            if (previous.sqrMagnitude < Mathf.Epsilon || current.sqrMagnitude < Mathf.Epsilon)
            {
                previousPointerPosition = currentPosition;
                return;
            }

            var deltaAngle = Mathf.Atan2(current.y, current.x) - Mathf.Atan2(previous.y, previous.x);
            var deltaDegrees = deltaAngle * Mathf.Rad2Deg;

            SetAngleWithoutNotify(angle + deltaDegrees);
            previousPointerPosition = currentPosition;

            var (hour, minute) = AngleToTime(angle);
            TimeChanged?.Invoke(hour, minute);
        }

        private static (int hour, int minute) AngleToTime(float inputAngle)
        {
            var normalizedAngle = inputAngle;
            normalizedAngle %= 360f;
            if (normalizedAngle < 0f) normalizedAngle += 360f;

            var hour = (int)(normalizedAngle / 15f) % 24;
            var minute = (int)((normalizedAngle % 15f) * 4f);
            return (hour, minute);
        }
    }
}




