using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class SliderRange : UnityEngine.UIElements.MinMaxSlider
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

        private VisualElement rangeRow;

        public SliderRange() : base(0f, 10f, 0f, 10f)
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            if (string.IsNullOrEmpty(label))
                label = "Label";

            RegisterCallback<AttachToPanelEvent>(_ => BuildRangeRow());
            // Update fields when thumbs move (MinMaxSlider value is Vector2)
            RegisterCallback<ChangeEvent<Vector2>>(_ => UpdateFieldsFromThumbs());
        }

        private void BuildRangeRow()
        {
            if (rangeRow != null)
                return;

            var input = this.Q<VisualElement>(className: "unity-min-max-slider__input");
            if (input == null)
                return;

            // Create a row container for Minfield - MinMaxSlider - MaxField
            rangeRow = new VisualElement { name = "RangeRow" };
            rangeRow.AddToClassList("slider-range__row");

            // Create min/max fields (no label, just value)
            minField = CreateRangeField("MinField", "min-field");
            maxField = CreateRangeField("MaxField", "max-field");

            RegisterFieldCallbacks();

            this.Add(rangeRow);

            // Reparent: remove input from current parent and add inside row
            input.RemoveFromHierarchy();

            rangeRow.Add(minField);
            rangeRow.Add(input);
            rangeRow.Add(maxField);

            ApplyInputFieldVisibility();
            UpdateFieldsFromThumbs();
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

        private NumberField minField;
        private NumberField maxField;

        private NumberField CreateRangeField(string name, string className)
        {
            var field = new NumberField
            {
                name = name,
                LabelVisible = false,
                DecimalCount = decimalCount,
                Style = NumberFieldStyle.Small,
                ScrollStep = EffectiveScrollStep
            };

            field.AddToClassList(className);
            return field;
        }

        private void RegisterFieldCallbacks()
        {
            minField.InputField.RegisterCallback<BlurEvent>(_ => ApplyFieldValues());
            minField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => ApplyFieldValues(), TrickleDown.TrickleDown);

            maxField.InputField.RegisterCallback<BlurEvent>(_ => ApplyFieldValues());
            maxField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => ApplyFieldValues(), TrickleDown.TrickleDown);
        }

        private void ApplyInputFieldVisibility()
        {
            minField?.EnableInClassList("hidden", !showNumberInputField);
            maxField?.EnableInClassList("hidden", !showNumberInputField);
        }

        private void ApplyFieldValues()
        {
            var newMin = Mathf.Clamp((float)minField.GetValueAsDouble(), lowLimit, maxValue);
            var newMax = Mathf.Clamp((float)maxField.GetValueAsDouble(), newMin, highLimit);

            value = new Vector2(newMin, newMax);
            UpdateFieldsFromThumbs();
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
            if (minField == null || maxField == null)
                return;

            minField.DecimalCount = decimalCount;
            maxField.DecimalCount = decimalCount;
            UpdateFieldsFromThumbs();
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
                scrollStep = value;
                ApplyScrollStep();
            }
        }

        private float EffectiveScrollStep =>
            scrollStep > 0f
                ? scrollStep
                : Mathf.Max((highLimit - lowLimit) / DefaultStepDivision, MinimumScrollStep);

        private void ApplyScrollStep()
        {
            var effectiveScrollStep = EffectiveScrollStep;

            if (minField != null)
                minField.ScrollStep = effectiveScrollStep;

            if (maxField != null)
                maxField.ScrollStep = effectiveScrollStep;
        }

        // Wire minField/maxField to minValue/maxValue.
        private void UpdateFieldsFromThumbs()
        {
            if (minField == null || maxField == null)
                return;

            minField.SetValueWithoutNotify(minValue);
            maxField.SetValueWithoutNotify(maxValue);
        }

        private void UpdateClassList()
        {
            this.ReplacePrefixedValueInClassList("slider-header-", headerType.ToString().ToKebabCase());
        }
    }
}