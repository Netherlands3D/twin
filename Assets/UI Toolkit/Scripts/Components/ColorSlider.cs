using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ColorSlider : Slider
    {
        public enum ColorSliderType
        {
            Value,
            Alpha
        }

        private VisualElement tracker;
        private VisualElement Tracker => tracker ??= this.Q<VisualElement>(className: "unity-base-slider__tracker");

        private VisualElement dragger;
        private VisualElement Dragger => dragger ??= this.Q<VisualElement>(className: "unity-base-slider__dragger");

        private VisualElement backgroundLayer;
        private VisualElement overlayLayer;
        private VisualElement draggerShadow;

        private ColorSliderType sliderType = ColorSliderType.Value;
        [UxmlAttribute("slider-type")]
        public ColorSliderType SliderType
        {
            get => sliderType;
            set
            {
                sliderType = value;
                UpdateBackgroundStyle();
            }
        }

        private string colorHex = "#FFC142";
        [UxmlAttribute("color")]
        public string Color
        {
            get => colorHex;
            set
            {
                colorHex = value;
                ApplyOverlayTint();
            }
        }

        public ColorSlider()
        {
            this.AddComponentStylesheet("Components");

            AddToClassList("color-slider");

            direction = SliderDirection.Vertical;
            showInputField = false;
            fill = false;

            lowValue = 0f;
            highValue = 1f;
            value = 1f;

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                BuildTrackerLayers();
                BuildDraggerShadow();
                UpdateBackgroundStyle();
                ApplyOverlayTint();
            });
        }

        private void BuildTrackerLayers()
        {
            if (Tracker == null || backgroundLayer != null || overlayLayer != null)
                return;

            backgroundLayer = new VisualElement();
            backgroundLayer.AddToClassList("color-slider__background");

            overlayLayer = new VisualElement();
            overlayLayer.AddToClassList("color-slider__overlay");

            Tracker.Add(backgroundLayer);
            Tracker.Add(overlayLayer);
        }

        private void BuildDraggerShadow()
        {
            if (Dragger == null || draggerShadow != null)
                return;

            var draggerParent = Dragger.parent;
            if (draggerParent == null)
                return;

            draggerShadow = new VisualElement();
            draggerShadow.AddToClassList("color-slider__dragger-shadow");
            draggerShadow.AddToClassList("shadow-sm");
            draggerShadow.pickingMode = PickingMode.Ignore;

            int draggerIndex = draggerParent.IndexOf(Dragger);
            draggerParent.Insert(draggerIndex, draggerShadow);
        }

        private void UpdateBackgroundStyle()
        {
            if (backgroundLayer == null)
                return;

            backgroundLayer.EnableInClassList("color-slider__background--alpha", sliderType == ColorSliderType.Alpha);
        }

        private void ApplyOverlayTint()
        {
            if (overlayLayer == null)
                return;

            if (ColorUtility.TryParseHtmlString(colorHex, out var parsedColor))
            {
                overlayLayer.style.unityBackgroundImageTintColor = parsedColor;
            }
        }
    }
}