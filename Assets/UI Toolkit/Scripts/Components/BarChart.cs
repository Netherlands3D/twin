using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// Displays non-negative, stacked columns. Callers supply data, colours and descriptive text.
    /// Null values have no segment; category tooltips are shown on total and category labels.
    /// </summary>
    [UxmlElement]
    public partial class BarChart : VisualElement
    {
        private const int IntervalCount = 4;
        private const decimal CompactThreshold = 9_999m;
        private readonly Header titleHeader;
        private readonly Label descriptionLabel;
        private readonly Label axisLabel;
        private readonly VisualElement axisTitle;
        private readonly Label emptyLabel;
        private readonly VisualElement body;
        private readonly VisualElement ticks;
        private readonly VisualElement columns;
        private readonly VisualElement legend;

        [UxmlAttribute("title")]
        public string Title { get => titleHeader.LabelText; set => titleHeader.LabelText = value; }

        [UxmlAttribute("icon")]
        public string Icon { get => titleHeader.Image; set => titleHeader.Image = value; }

        [UxmlAttribute("description")]
        public string Description
        {
            get => descriptionLabel.text;
            set
            {
                descriptionLabel.text = value;
                descriptionLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("value-axis-label")]
        public string ValueAxisLabel
        {
            get => axisLabel.text;
            set
            {
                axisLabel.text = value;
                axisTitle.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("empty-text")]
        public string EmptyText { get => emptyLabel.text; set => emptyLabel.text = value; }

        [UxmlAttribute("minimum-step")]
        public double MinimumStep { get; set; }

        [UxmlAttribute("compact-numbers")]
        public bool CompactNumbers { get; set; }

        [UxmlAttribute("thousand-suffix")]
        public string ThousandSuffix { get; set; } = "k";

        [UxmlAttribute("million-suffix")]
        public string MillionSuffix { get; set; } = "M";

        [UxmlAttribute("billion-suffix")]
        public string BillionSuffix { get; set; } = "B";

        /// <summary>Formatting used for numeric axis labels on the next SetData call.</summary>
        public string ValueFormat { get; set; } = "#,0.##";
        public CultureInfo NumberCulture { get; set; } = CultureInfo.CurrentCulture;

        public BarChart()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            titleHeader = this.Q<Header>("ChartTitle");
            descriptionLabel = this.Q<Label>("ChartDescription");
            axisLabel = this.Q<Label>("ValueAxisLabel");
            axisTitle = this.Q("AxisTitle");
            emptyLabel = this.Q<Label>("EmptyChart");
            body = this.Q("ChartBody");
            ticks = this.Q("AxisTicks");
            columns = this.Q("Columns");
            legend = this.Q("ChartLegend");
            Description = descriptionLabel.text;
            ValueAxisLabel = axisLabel.text;
        }

        /// <summary>Replaces all columns. Every category must contain one value per series, in series order.</summary>
        public void SetData(IReadOnlyList<Series> series, IReadOnlyList<Category> categories)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            if (double.IsNaN(MinimumStep) || double.IsInfinity(MinimumStep) || MinimumStep < 0)
                throw new ArgumentOutOfRangeException(nameof(MinimumStep));
            if (series.Any(item => item == null))
                throw new ArgumentException("Series cannot contain null entries.", nameof(series));
            foreach (var category in categories)
            {
                if (category == null || category.Values.Count != series.Count)
                    throw new ArgumentException("Each category must contain one value per series.", nameof(categories));
                if (category.Values.Any(value => value.Amount < 0))
                    throw new ArgumentException("This chart supports non-negative values only.", nameof(categories));
            }

            columns.Clear();
            ticks.Clear();
            legend.Clear();
            var hasData = categories.Count > 0 && series.Count > 0;
            body.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            legend.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            emptyLabel.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;
            if (!hasData)
                return;

            var stacks = categories.Select(category => (Category: category, Total: category.Values.Sum(value => value.Amount ?? 0m))).ToArray();
            var largestStack = (double)stacks.Max(stack => stack.Total);
            var step = Math.Max(CalculateStep(largestStack), MinimumStep);
            var maximum = step * IntervalCount;
            for (var i = 0; i <= IntervalCount; i++)
            {
                var tickValue = step * i;
                var tick = new Label(FormatAxisValue(tickValue)) { tooltip = tickValue.ToString(ValueFormat, NumberCulture) };
                tick.AddToClassList("bar-chart__tick");
                tick.style.bottom = Length.Percent(100f * i / IntervalCount);
                ticks.Add(tick);
            }

            foreach (var item in stacks)
            {
                var category = item.Category;
                var column = new VisualElement();
                column.AddToClassList("bar-chart__column");
                var plot = new VisualElement { pickingMode = PickingMode.Ignore };
                plot.AddToClassList("bar-chart__plot");
                for (var i = 0; i <= IntervalCount; i++)
                {
                    var line = new VisualElement { pickingMode = PickingMode.Ignore };
                    line.AddToClassList("bar-chart__grid-line");
                    // Keep the top border inside the viewport instead of drawing it above the clipping edge.
                    if (i == IntervalCount)
                        line.style.top = 0;
                    else
                        line.style.bottom = Length.Percent(100f * i / IntervalCount);
                    plot.Add(line);
                }

                var stack = new VisualElement { pickingMode = PickingMode.Ignore };
                stack.AddToClassList("bar-chart__stack");
                for (var i = 0; i < series.Count; i++)
                {
                    var value = category.Values[i];
                    if (!value.Amount.HasValue || value.Amount.Value == 0)
                        continue;

                    var segment = new VisualElement { tooltip = value.Tooltip };
                    segment.AddToClassList("bar-chart__segment");
                    segment.style.height = Length.Percent((float)((double)value.Amount.Value / maximum * 100));
                    segment.style.backgroundColor = series[i].Color;
                    stack.Add(segment);
                }

                plot.Add(stack);
                var total = item.Total;
                var totalText = category.TotalLabel ?? (category.Values.All(value => value.Amount.HasValue)
                    ? FormatValue(total)
                    : "\u2014");
                var totalLabel = new Label(totalText) { tooltip = category.Tooltip };
                totalLabel.AddToClassList("bar-chart__total-label");
                var totalAnchor = new VisualElement { pickingMode = PickingMode.Ignore };
                totalAnchor.AddToClassList("bar-chart__total-anchor");
                totalAnchor.style.bottom = Length.Percent((float)((double)total / maximum * 100));
                totalAnchor.Add(totalLabel);
                plot.Add(totalAnchor);
                plot.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    if (evt.newRect.height <= 0)
                        return;

                    var labelSpace = totalAnchor.resolvedStyle.height + totalAnchor.resolvedStyle.marginBottom;
                    var spaceAboveTallestStack = evt.newRect.height * (float)(1 - largestStack / maximum);
                    var topMargin = Mathf.Max(0, labelSpace - spaceAboveTallestStack);
                    if (!Mathf.Approximately(plot.resolvedStyle.marginTop, topMargin))
                        plot.style.marginTop = topMargin;
                    if (!Mathf.Approximately(ticks.parent.resolvedStyle.marginTop, topMargin))
                        ticks.parent.style.marginTop = topMargin;
                });
                column.Add(plot);
                var label = new Label(category.Label) { tooltip = category.Tooltip };
                label.AddToClassList("bar-chart__category-label");
                column.Add(label);
                columns.Add(column);
            }

            foreach (var item in series)
            {
                var entry = new VisualElement();
                entry.AddToClassList("bar-chart__legend-entry");
                var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
                swatch.AddToClassList("bar-chart__swatch");
                swatch.style.backgroundColor = item.Color;
                entry.Add(swatch);
                entry.Add(new Label(item.Label));
                legend.Add(entry);
            }
        }

        /// <summary>Formats a value for totals and axes. Tooltips can retain the original, unabridged value.</summary>
        public string FormatValue(decimal value)
        {
            var magnitude = Math.Abs(value);
            if (!CompactNumbers || magnitude < CompactThreshold)
                return value.ToString(ValueFormat, NumberCulture);

            var divisor = magnitude >= 1_000_000_000m ? 1_000_000_000m : magnitude >= 1_000_000m ? 1_000_000m : 1_000m;
            var rounded = decimal.Round(value / divisor, 1, MidpointRounding.AwayFromZero);
            if (Math.Abs(rounded) >= 1_000m && divisor < 1_000_000_000m)
            {
                divisor *= 1_000m;
                rounded = decimal.Round(value / divisor, 1, MidpointRounding.AwayFromZero);
            }

            var suffix = divisor == 1_000m ? ThousandSuffix : divisor == 1_000_000m ? MillionSuffix : BillionSuffix;
            return rounded.ToString("0.#", NumberCulture) + suffix;
        }

        private string FormatAxisValue(double value)
        {
            if (value < (double)decimal.MaxValue)
                return FormatValue((decimal)value);

            return CompactNumbers
                ? (value / 1_000_000_000).ToString("0.#", NumberCulture) + BillionSuffix
                : value.ToString(ValueFormat, NumberCulture);
        }

        private static double CalculateStep(double maximum)
        {
            if (maximum <= 0)
                return 1;

            var roughStep = maximum / IntervalCount;
            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(roughStep)));
            var fraction = roughStep / magnitude;
            var multiplier = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 5 ? 5 : 10;
            return magnitude * multiplier;
        }

        public sealed class Series
        {
            public string Label { get; }
            public Color Color { get; }

            public Series(string label, Color color)
            {
                Label = label;
                Color = color;
            }
        }

        public readonly struct Value
        {
            public decimal? Amount { get; }
            public string Tooltip { get; }

            public Value(decimal? amount, string tooltip = null)
            {
                Amount = amount;
                Tooltip = tooltip;
            }
        }

        public sealed class Category
        {
            public string Label { get; }
            public string Tooltip { get; }

            /// <summary>Optional total text, including any completeness marker supplied by the caller.</summary>
            public string TotalLabel { get; }
            public IReadOnlyList<Value> Values { get; }

            public Category(string label, IEnumerable<Value> values, string tooltip = null) : this(label, values, tooltip, null)
            {
            }

            public Category(string label, IEnumerable<Value> values, string tooltip, string totalLabel)
            {
                Label = label;
                Tooltip = tooltip;
                TotalLabel = totalLabel;
                Values = Array.AsReadOnly((values ?? throw new ArgumentNullException(nameof(values))).ToArray());
            }
        }
    }
}
