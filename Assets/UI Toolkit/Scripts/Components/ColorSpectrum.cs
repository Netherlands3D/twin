using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ColorSpectrum : VisualElement
    {
        private VisualElement thumb;
        private VisualElement Thumb => thumb ??= this.Q<VisualElement>("Thumb");
        private Vector2 dragStartPosition;

        private Vector2 center => new Vector2(resolvedStyle.width / 2, resolvedStyle.height / 2);
        private float colorSpectrumRadius = 80f;

        private Vector2 selectorPosition = new Vector2(80f, 80f);

        [UxmlAttribute("selector-position")]
        public Vector2 SelectorPosition
        {
            get => selectorPosition;
            set
            {
                var offset = value - center;
                selectorPosition = value;
                if (offset.magnitude > colorSpectrumRadius)
                    selectorPosition = center + (offset.normalized * colorSpectrumRadius);

                ApplySelectorPosition();
            }
        }

        private float hue;
        public float Hue
        {
            get => hue;
            private set
            {
                hue = value;
                HueChanged.Invoke(hue);
            }
        }

        private float saturation;
        public float Saturation
        {
            get => saturation;
            private set
            {
                saturation = value; 
                SaturationChanged.Invoke(saturation);
            }
        }

        public UnityEvent<float> HueChanged = new();
        public UnityEvent<float> SaturationChanged = new();
        public UnityEvent SpectrumChanged = new();

        public ColorSpectrum()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            AddToClassList("color-spectrum");
            RegisterCallback<AttachToPanelEvent>(_ => ApplySelectorPosition());

            var dragManipulator = new DragManipulator();
            dragManipulator.DragStarted.AddListener(OnDragStarted);
            dragManipulator.Dragging.AddListener(OnDragging);
            this.AddManipulator(dragManipulator);
        }

        private void OnDragStarted(Vector2 startPosition)
        {
            dragStartPosition = startPosition;
            SelectorPosition = startPosition;
        }

        private void OnDragging(Vector2 delta)
        {
            SelectorPosition = dragStartPosition + delta;
        }

        private void ApplySelectorPosition()
        {
            if (Thumb == null)
                return;

            Thumb.style.left = SelectorPosition.x;
            Thumb.style.top = SelectorPosition.y;

            var top = new Vector2(center.x, colorSpectrumRadius);
            var angle = Vector2.Angle(top - center, SelectorPosition - center);

            //if the selector is on the left side of the spectrum, the angle calculates the angle on the left side instead of the outer angle needed for the hue value
            if (selectorPosition.x < center.x)
                angle = 360f - angle;

            Hue = angle;
            Saturation = Vector2.Distance(center, selectorPosition) / colorSpectrumRadius;
            SpectrumChanged.Invoke();
        }
    }
}