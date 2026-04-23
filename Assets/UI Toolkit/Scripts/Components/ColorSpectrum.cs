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
        private VisualElement spectrum;
        private VisualElement Spectrum => spectrum ??= this.Q<VisualElement>("Surface"); 
        private Vector2 dragStartPosition;

        private Vector2 center => new Vector2(resolvedStyle.width / 2, resolvedStyle.height / 2);
        private const float colorSpectrumRadius = 80f;

        private Vector2 selectorPosition = new Vector2(colorSpectrumRadius, colorSpectrumRadius);

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

        private float brightness;

        public float Brightness
        {
            get => brightness;
            set
            {
                brightness = value;
                ApplyOverlayTint();
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

            SetThumbPosition(SelectorPosition);
            
            var top = new Vector2(0, -colorSpectrumRadius);
            var angle = Vector2.Angle(top, SelectorPosition - center);

            //if the selector is on the left side of the spectrum, the angle calculates the angle on the left side instead of the outer angle needed for the hue value
            if (selectorPosition.x < center.x)
                angle = 360f - angle;

            Hue = angle;
            Saturation = Vector2.Distance(center, selectorPosition) / colorSpectrumRadius;
            SpectrumChanged.Invoke();
        }

        public void SetValueWithoutNotify(float hue, float saturation)
        {
            this.hue = hue;
            this.saturation = saturation;

            var angle = (hue - 90f) * Mathf.Deg2Rad; //red is at the top so the whole spectrum is rotated by 90 degrees
            var distance = saturation * colorSpectrumRadius;

            selectorPosition = center + new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );

            SetThumbPosition(selectorPosition);
        }

        private void SetThumbPosition(Vector2 position)
        {
            Thumb.style.left = position.x;
            Thumb.style.top = position.y;
        }

        private void ApplyOverlayTint()
        {
            if (Spectrum == null)
                return;

            Spectrum.style.unityBackgroundImageTintColor = Color.HSVToRGB(0, 0, brightness);
        }
    }
}