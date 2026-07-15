using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Slider : UnityEngine.UIElements.Slider
    {
        public enum HeaderType
        {
            Normal,
            NoHeader
        }

        private HeaderType headerType = HeaderType.Normal;

        [UxmlAttribute("header-type")]
        public HeaderType Header
        {
            get => headerType;
            set { headerType = value; UpdateClassList(); }
        }

        public Slider()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            if (string.IsNullOrEmpty(label))
                label = "Label";

            showInputField = false;
            fill = true;

            RegisterCallback<AttachToPanelEvent>(_ => BuildSliderRow());
            RegisterCallback<ChangeEvent<float>>(_ => UpdateInputFieldFromSlider());
        }

        private VisualElement sliderRow;

        private void BuildSliderRow()
        {
            if (sliderRow != null)
                return;

            var input = this.Q<VisualElement>(className: "unity-base-slider__input");
            if (input == null)
            {
                Debug.LogError(
                    $"{nameof(Slider)} could not find Unity's internal slider input."
                );
                return;
            }

            sliderRow = new VisualElement { name = "SliderRow" };
            sliderRow.AddToClassList("slider__row");

            inputField = CreateInputField();

            this.Add(sliderRow);

            input.RemoveFromHierarchy();

            showInputField = false;
            sliderRow.Add(inputField);
            sliderRow.Add(input);

            input.RegisterCallback<WheelEvent>(OnSliderScroll, TrickleDown.TrickleDown);

            ApplyInputFieldVisibility();
            UpdateInputFieldFromSlider();
        }

        private bool showNumberInputField = true;

        [UxmlAttribute("show-number-field")]
        public bool ShowNumberInputField
        {
            get => showNumberInputField;
            set
            {
                showNumberInputField = value;
                ApplyInputFieldVisibility();
            }
        }

        private NumberField inputField;

        private NumberField CreateInputField()
        {
            var field = new NumberField
            {
                name = "InputField",
                LabelVisible = false,
                DecimalCount = decimalCount,
                Style = NumberFieldStyle.Small,
                ScrollStep = EffectiveScrollStep
            };

            field.AddToClassList("slider__input-field");

            field.InputField.RegisterCallback<BlurEvent>(_ => ApplyInputFieldValue());
            field.InputField.RegisterCallback<NavigationSubmitEvent>(_ => ApplyInputFieldValue(), TrickleDown.TrickleDown);

            return field;
        }

        private void ApplyInputFieldVisibility()
        {
            inputField?.EnableInClassList("hidden", !showNumberInputField);
        }

        private void ApplyInputFieldValue()
        {
            value = Mathf.Clamp((float)inputField.GetValueAsDouble(), lowValue, highValue);
            UpdateInputFieldFromSlider();
        }

        private int decimalCount = 2;

        [UxmlAttribute("decimal-count")]
        public int DecimalCount
        {
            get => decimalCount;
            set
            {
                decimalCount = value;
                ApplyDecimalCount();
            }
        }

        private void ApplyDecimalCount()
        {
            if (inputField == null)
                return;

            inputField.DecimalCount = decimalCount;
            UpdateInputFieldFromSlider();
        }

        private const float DefaultStepDivision = 100f;
        private const float MinimumScrollStep = 0.01f;

        private float scrollStep = 0f;

        [UxmlAttribute("scroll-step")]
        public float ScrollStep
        {
            get => scrollStep;
            set
            {
                scrollStep = Mathf.Max(0f, value);
                ApplyScrollStep();
            }
        }

        private float EffectiveScrollStep =>
            scrollStep > 0f
                ? scrollStep
                : Mathf.Max((highValue - lowValue) / DefaultStepDivision, MinimumScrollStep);

        private void ApplyScrollStep()
        {
            if (inputField == null)
                return;

            inputField.ScrollStep = EffectiveScrollStep;
        }

        private void UpdateInputFieldFromSlider()
        {
            if (inputField == null)
                return;

            inputField.SetValueWithoutNotify(value);
        }

        private void OnSliderScroll(WheelEvent evt)
        {
            var direction = evt.delta.y > 0f ? -1f : 1f;
            value = Mathf.Clamp(value + direction * EffectiveScrollStep, lowValue, highValue);
            evt.StopPropagation();
        }

        private void UpdateClassList()
        {
            this.ReplacePrefixedValueInClassList("slider-header-", headerType.ToString().ToKebabCase());
        }
    }
}