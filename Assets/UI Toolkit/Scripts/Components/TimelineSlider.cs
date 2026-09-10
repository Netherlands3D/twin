using System;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class TimelineSlider : VisualElement
    {
        private float maxDragDistance = 100f;
        private float maxScrubSpeed = 2f;
        
        private Slider slider;
        private VisualElement scrubber;
        private Label currentTimeLabel;
        private NumberField minField;
        private NumberField maxField;

        private bool isDragging;
        private float dragStartX;
        private float currentDragOffset;
        // private IVisualElementScheduledItem scrubTicker;
        
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
        
        public TimelineSlider()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            slider = this.Q<Slider>("Timeline");
            scrubber = this.Q<VisualElement>("Scrubber");
            currentTimeLabel = scrubber.Q<Label>("CurrentTimeLabel");
            // minField = this.Q<NumberField>("MinField");
            // maxField = this.Q<NumberField>("MaxField");
             
            SetInitialDate();
            // InitBoundsFields();
            
            RegisterCallback<AttachToPanelEvent>(ApplyTextFieldClassToInput);
            slider.RegisterValueChangedCallback(OnSliderChanged);
            var dragManipulator = new DragManipulator(4);
            scrubber.AddManipulator(dragManipulator);
            dragManipulator.DragStarted.AddListener(OnScrubberDragStart);
            dragManipulator.Dragging.AddListener(OnScrubberDrag);
            dragManipulator.DragEnded.AddListener(OnScrubberDragEnd);
            
            if(Application.isPlaying)
                schedule.Execute(UpdateSlider).Every(0);
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
            var speed = eased * MaxScrubSpeed;
            var newValue = Mathf.Clamp(slider.value + speed * Time.deltaTime, slider.lowValue, slider.highValue);
            slider.value = newValue;
        }
        
        private void OnScrubberDragEnd(Vector2 endPosition)
        {
            currentDragOffset = 0f;
            isDragging = false;
        }
        
        private void InitBoundsFields()
        {
            minField.SetValueWithoutNotify(slider.lowValue);
            maxField.SetValueWithoutNotify(slider.highValue);

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
            int year = Mathf.FloorToInt(evt.newValue);
            float fraction = evt.newValue - year;
            int totalDays = DateTime.IsLeapYear(year) ? 366 : 365;
            int dayOfYear = Mathf.Clamp(Mathf.FloorToInt(fraction * totalDays), 0, totalDays - 1);

            DateTime date = new DateTime(year, 1, 1).AddDays(dayOfYear);

            SetDate(date);
        }


        private void SetDate(DateTime dateTime)
        {
            ServiceLocator.GetService<SunTime>().SetDate(dateTime.Day, dateTime.Month,  dateTime.Year);
            currentTimeLabel.text = dateTime.ToString("MM/dd/yyyy");
        }

        
        void SetInitialDate()
        {
            var dateTime = ServiceLocator.GetService<SunTime>()?.Time;
            if(dateTime.HasValue)
                SetDate(dateTime.Value);
        }

        
        private void ApplyTextFieldClassToInput(AttachToPanelEvent evt)
        {
            // var inputText = this.Q<TextElement>(className: "unity-text-element--inner-input-field-component");
            // if (inputText == null)
            //     return;
            //
            // inputText.AddToClassList("text-base");
            //
            // var input = this.Q<VisualElement>(className: "unity-base-text-field");
            // if (input == null)
            //     return;
            //
            // input.AddToClassList("textfield");
        }
    }
}