using System;
using System.Globalization;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TimelineSlider : VisualElement
    {
        private SunTime sunTime;
        private float maxDragDistance = 100f;
        private float maxScrubSpeed = 1f;

        private Slider slider;
        private VisualElement scrubber;
        private Label currentTimeLabel;
        private NumberField minField;
        private NumberField maxField;

        private bool isDragging;
        private float dragStartX;
        private float currentDragOffset;

        [UxmlAttribute("max-drag-distance")]
        public float MaxDragDistance
        {
            get => maxDragDistance;
            set => maxDragDistance = value;
        }

        [UxmlAttribute("max-scrub-speed")]
        public float MaxScrubSpeed
        {
            get => maxScrubSpeed;
            set => maxScrubSpeed = value;
        }

        private DateTime minDateTime;
        public DateTime MinDateTime => minDateTime;

        [UxmlAttribute("min-datetime")]
        public string MinDateTimeString
        {
            get => minDateTime.ToString();
            set
            {
                var result = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                minDateTime = result;
            }
        }

        private DateTime maxDateTime;
        public DateTime MaxDateTime => maxDateTime;

        [UxmlAttribute("max-datetime")]
        public string MaxDateTimeString
        {
            get => maxDateTime.ToString();
            set
            {
                var result = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                maxDateTime = result;
            }
        }

        public TimelineSlider()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            slider = this.Q<Slider>("Timeline");
            slider.lowValue = 0;
            slider.highValue = 1;
            scrubber = this.Q<VisualElement>("Scrubber");
            currentTimeLabel = scrubber.Q<Label>("CurrentTimeLabel");

            minField = this.Q<NumberField>("MinField");
            maxField = this.Q<NumberField>("MaxField");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            
            slider.RegisterValueChangedCallback(OnSliderChanged);
            var dragManipulator = new DragManipulator(4);
            scrubber.AddManipulator(dragManipulator);
            dragManipulator.DragStarted.AddListener(OnScrubberDragStart);
            dragManipulator.Dragging.AddListener(OnScrubberDrag);
            dragManipulator.DragEnded.AddListener(OnScrubberDragEnd);

            if (Application.isPlaying)
            {
                ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);
                schedule.Execute(UpdateSlider).Every(0);
            }
        }

        private void OnProjectDataChanged(ProjectData projectData)
        {
            SetInitialDate();
        }

        private void OnScrubberDragStart(Vector2 startPosition)
        {
            currentDragOffset = 0f;
            isDragging = true;
        }

        private void OnScrubberDrag(Vector2 delta)
        {
            currentDragOffset += delta.x;
            scrubber.style.translate = new Translate(Mathf.Clamp(currentDragOffset, -MaxDragDistance, MaxDragDistance), 0);
        }

        private void UpdateSlider()
        {
            var normalized = Mathf.Clamp(currentDragOffset / MaxDragDistance, -1f, 1f);
            var eased = Mathf.Sign(normalized) * normalized * normalized;
            var speed = eased / MaxScrubSpeed;
            slider.value = Mathf.Clamp01(slider.value +speed*Time.deltaTime);
        }

        private void OnScrubberDragEnd(Vector2 endPosition)
        {
            currentDragOffset = 0f;
            scrubber.style.translate = new Translate(Mathf.Clamp(currentDragOffset, -MaxDragDistance, MaxDragDistance), 0);
            isDragging = false;
        }

        private void InitRangeFields()
        {
            var range = maxDateTime - minDateTime;
            if (range.Days > 1)
            {
                minField.ValueFormat = NumberFieldFormat.Date;
                maxField.ValueFormat = NumberFieldFormat.Date;
            }
            else
            {
                minField.ValueFormat = NumberFieldFormat.Time;
                maxField.ValueFormat = NumberFieldFormat.Time;
            }
            minField.SetValueWithoutNotify(minDateTime);
            maxField.SetValueWithoutNotify(maxDateTime);
            
            minField.SetEnabled(false);
            maxField.SetEnabled(false);

            // minField.RegisterValueChangedCallback(OnMinFieldChanged);
            // maxField.RegisterValueChangedCallback(OnMaxFieldChanged);
        }

        private void OnMinFieldChanged(ChangeEvent<float> evt)
        {
            var newValue = Mathf.Min(evt.newValue, slider.highValue);
            slider.lowValue = newValue;
            minField.SetValueWithoutNotify(newValue);
        }

        private void OnMaxFieldChanged(ChangeEvent<float> evt)
        {
            var newMax = Mathf.Max(evt.newValue, slider.lowValue);
            slider.highValue = newMax;
            maxField.SetValueWithoutNotify(newMax);
        }

        private void OnSliderChanged(ChangeEvent<float> evt)
        {
            SetDate(SliderValueToDateTime(evt.newValue));
        }
        

        private DateTime SliderValueToDateTime(float value)
        {
            var totalSeconds = (maxDateTime - minDateTime).TotalSeconds;
            return minDateTime.AddSeconds(value * totalSeconds);
        }

        private float DateTimeToSliderValue(DateTime dateTime)
        {
            var totalSeconds = (maxDateTime - minDateTime).TotalSeconds;
            return (float)((dateTime - minDateTime).TotalSeconds / totalSeconds);
        }

        private void SetDate(DateTime dateTime)
        {
            sunTime.SetDate(dateTime.Day, dateTime.Month, dateTime.Year);
            sunTime.SetTime(dateTime.Hour, dateTime.Minute, dateTime.Second);
            currentTimeLabel.text = dateTime.ToString("MM/dd/yyyy HH:mm");
        }

        void SetInitialDate()
        {
            var dateTime = sunTime.Time;
            SetDate(dateTime);
            var sliderValue = DateTimeToSliderValue(dateTime);
            slider.SetValueWithoutNotify(sliderValue);
        }


        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if(Application.isPlaying)
            {
                sunTime = ServiceLocator.GetService<SunTime>();
                SetInitialDate();
            }
            InitRangeFields();
        }
    }
}