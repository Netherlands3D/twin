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

        private Vector2 dragStartPosition;
        private Vector2 previousPointerPosition;
        private float angle;

        public event Action<int, int> TimeChanged;

        private VisualElement DialFace => dialFace ??= this.Q<VisualElement>("DialFace");
        private VisualElement OrbitContainer => orbitContainer ??= this.Q<VisualElement>("OrbitContainer");
        private DragManipulator DragManipulator => dragManipulator ??= new DragManipulator();

        public SunDial()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            DialFace.AddManipulator(DragManipulator);
            DragManipulator.DragStarted.AddListener(OnDragStarted);
            DragManipulator.Dragging.AddListener(OnDragging);
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
            dragStartPosition = startPosition;
            previousPointerPosition = startPosition;
        }

        private void OnDragging(Vector2 deltaFromStart)
        {
            var center = new Vector2(
                OrbitContainer.layout.x + OrbitContainer.layout.width * 0.5f,
                OrbitContainer.layout.y + OrbitContainer.layout.height * 0.5f
            );
            var currentPosition = dragStartPosition + deltaFromStart;
            var previous = previousPointerPosition - center;
            var current = currentPosition - center;

            if (previous.sqrMagnitude < Mathf.Epsilon || current.sqrMagnitude < Mathf.Epsilon)
            {
                previousPointerPosition = currentPosition;
                return;
            }

            var delta = Mathf.Atan2(current.y, current.x) - Mathf.Atan2(previous.y, previous.x);
            var deltaDegrees = delta * Mathf.Rad2Deg;

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




