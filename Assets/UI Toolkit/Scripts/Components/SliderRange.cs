using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class SliderRange : UnityEngine.UIElements.MinMaxSlider
    {
        private VisualElement rangeRow;
        private FloatField minField;
        private FloatField maxField;

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

        public SliderRange()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            if (string.IsNullOrEmpty(label))
                label = "Label";

            lowLimit = 0f;
            highLimit = 10f;

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
            minField = new FloatField { name = "MinField", label = string.Empty };
            maxField = new FloatField { name = "MaxField", label = string.Empty };

            ApplyTextFieldClasses(minField);
            ApplyTextFieldClasses(maxField);

            this.Add(rangeRow);

            // Reparent: remove input from current parent and add inside row
            input.RemoveFromHierarchy();

            rangeRow.Add(minField);
            rangeRow.Add(input);
            rangeRow.Add(maxField);

            UpdateFieldsFromThumbs();
        }

        // Wire minField/maxField to minValue/maxValue.
        private void UpdateFieldsFromThumbs()
        {
            if (minField == null || maxField == null)
                return;

            minField.SetValueWithoutNotify(minValue);
            maxField.SetValueWithoutNotify(maxValue);

            // TODO: Limit displayed decimals to 2 for MinField/MaxField.
        }

        private void ApplyTextFieldClasses(BaseField<float> field)
        {
            var baseTextField = field.Q<VisualElement>(className: "unity-base-text-field");
            baseTextField?.AddToClassList("textfield");

            var innerText = field.Q<TextElement>(className: "unity-text-element--inner-input-field-component");
            innerText?.AddToClassList("text-base");

            minField.AddToClassList("min-field");
            maxField.AddToClassList("max-field");
        }

        private void UpdateClassList()
        {
            this.ReplacePrefixedValueInClassList("slider-header-", headerType.ToString().ToKebabCase());
        }
    }
}