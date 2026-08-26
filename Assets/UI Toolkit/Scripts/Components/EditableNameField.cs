using System.Globalization;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class EditableNameField : VisualElement, INotifyValueChanged<string>
    {
        private Label label; // we will switch between label and input field
        private TextField inputField;
        
        private bool firstClickDone;
        private bool intervalExpired;
        private IVisualElementScheduledItem clickTimer;
        [UxmlAttribute] public float ClickInterval { get; set; } = 0.5f;
        
        [UxmlAttribute("value")]
        public string value
        {
            get { return label.text; }
            set
            {
                if (label.text == value) return;
                using var evt = ChangeEvent<string>.GetPooled(label.text, value);
                evt.target = this;
                label.text = value;
                inputField.SetValueWithoutNotify(value);
                CalculateOverflow();
                SendEvent(evt);
            }
        }

        public bool IsEditing => label.ClassListContains(UtilityClassConstants.HIDDEN);
        
        private VisualElement labelContainer;

        private IVisualElementScheduledItem tickerSchedule;

        private float textWidth;
        private float availableWidth;
        private float ScrollSpeed = 60f;
        private float scrollPosition;
        private bool isOverflowing;
        
        public void SetValueWithoutNotify(string newValue)
        {
            label.text = newValue;
            inputField.SetValueWithoutNotify(newValue);
            CalculateOverflow();
        }
        
        public EditableNameField()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            label = this.Q<Label>("Label");
            RegisterCallback<PointerEnterEvent>(OnLabelHoverEnter);
            RegisterCallback<PointerLeaveEvent>(OnLabelHoverExit);
            RegisterCallback<GeometryChangedEvent>(OnLabelGeometryChanged);
            
            label.focusable = true;
            inputField = this.Q<TextField>("InputField");

            label.RegisterCallback<ClickEvent>(OnNameLabelClicked);
            label.RegisterCallback<BlurEvent>(OnLabelBlur);

            inputField.RegisterCallback<BlurEvent>(OnNameInputFieldBlur, TrickleDown.TrickleDown);
            inputField.RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmitted, TrickleDown.TrickleDown);

            inputField.EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }

        private void OnLabelBlur(BlurEvent evt)
        {
            ResetClickState();
        }

        private void ResetClickState()
        {
            firstClickDone = false;
            intervalExpired = false;
            clickTimer?.Pause();
        }

        private void StartEditing()
        {
            label.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            inputField.EnableInClassList(UtilityClassConstants.HIDDEN, false);

            inputField.Focus();
        }
        
        private void StopEditing()
        {
            label.EnableInClassList(UtilityClassConstants.HIDDEN, false);
            inputField.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            
            ResetClickState();
            value = inputField.text;
        }
        
        private void OnNameLabelClicked(ClickEvent evt)
        {
            if (!firstClickDone) 
            {
                //first click: start timer
                firstClickDone = true;
                intervalExpired = false;
                clickTimer = schedule.Execute(() => intervalExpired = true);
                clickTimer.ExecuteLater((long)(ClickInterval * 1000));
            }
            else if (intervalExpired)
            {
                // Second click after interval = start editing
                firstClickDone = false;
                intervalExpired = false;
                StartEditing();
            }
            // If neither cases are true: Second click before interval = double-click
        }

        private void OnNameInputFieldBlur(BlurEvent evt)
        {
            StopEditing();
        }

        private void OnNavigationSubmitted(NavigationSubmitEvent evt)
        {
            StopEditing();
        }
        
        private void OnLabelGeometryChanged(GeometryChangedEvent evt)
        {
            CalculateOverflow();
        }
        
        private void CalculateOverflow()
        {
            if (label == null)
                return;

            textWidth = label.MeasureTextSize(
                label.text,
                float.PositiveInfinity,
                MeasureMode.Undefined,
                label.resolvedStyle.height,
                MeasureMode.Exactly
            ).x;


            availableWidth = resolvedStyle.width;
            ResetTicker();
        }
        
        private void OnLabelHoverEnter(PointerEnterEvent evt)
        {
            if(textWidth < availableWidth) return;
            
            StartTicker();
        }
        
        private void OnLabelHoverExit(PointerLeaveEvent evt)
        {
            StopTicker();
        }

        private void StartTicker()
        {
            if (tickerSchedule != null)
                return;

            scrollPosition = 0;

            tickerSchedule = schedule.Execute(() =>
            {
                scrollPosition -= ScrollSpeed * Time.deltaTime;
                if (scrollPosition < -textWidth)
                    scrollPosition = availableWidth;

                label.style.translate = new Translate(
                    scrollPosition,
                    0,
                    0
                );

            });
            tickerSchedule.Every(0);
        }
        
        private void StopTicker()
        {
            tickerSchedule?.Pause();
            tickerSchedule = null;

            ResetTicker();
        }


        private void ResetTicker()
        {
            scrollPosition = 0;

            if(label != null)
            {
                label.style.translate = new Translate(0,0,0);
            }
        }
    }
}