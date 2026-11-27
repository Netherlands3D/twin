using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>
    /// Displays non-negative, stacked bars in either direction. Callers supply data, colours and descriptive text.
    /// Null values have no segment; category tooltips are shown on total and category labels.
    /// </summary>
    [UxmlElement]
    public partial class BarChart : VisualElement
    {
        public enum BarChartOrientation
        {
            Vertical,
            Horizontal
        }

        private const int IntervalCount = 4;
        private static readonly double[] StepMultipliers = { 1, 1.25, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10 };
        private readonly Header titleHeader;
        private readonly Label descriptionLabel;
        private readonly Label axisLabel;
        private readonly VisualElement axisTitle;
        private readonly Label emptyLabel;
        private readonly VisualElement body;
        private readonly VisualElement ticks;
        private readonly VisualElement columns;
        private readonly VisualElement legend;
        private readonly VisualElement valueAxis;
        private readonly VisualElement horizontalPlotArea;
        private readonly UnityEngine.UIElements.ScrollView chartScrollView;
        private readonly List<(VisualElement Plot, VisualElement Anchor, double Fraction)> plots = new();
        private readonly List<Label> categoryLabels = new();
        private Series[] seriesData = Array.Empty<Series>();
        private Category[] categoryData = Array.Empty<Category>();
        private BarChartOrientation orientation;
        private bool layoutUpdatePending;
        private bool IsHorizontal => orientation == BarChartOrientation.Horizontal;

        /// <summary>Changes the direction and rebuilds the current data. Vertical is the default.</summary>
        [UxmlAttribute("orientation")]
        public BarChartOrientation Orientation
        {
            get => orientation;
            set
            {
                if (value != BarChartOrientation.Vertical && value != BarChartOrientation.Horizontal)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (orientation == value)
                    return;

                orientation = value;
                if (body != null)
                    Render();
            }
        }

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

        /// <summary>Minimum number of positions reserved for vertical bars on the next SetData call.</summary>
        [UxmlAttribute("minimum-column-count")]
        public int MinimumColumnCount { get; set; }

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
            valueAxis = this.Q(className: "bar-chart__axis");
            chartScrollView = this.Q<UnityEngine.UIElements.ScrollView>("ChartScrollView");
            horizontalPlotArea = this.Q("HorizontalPlotArea");
            ticks.RegisterCallback<GeometryChangedEvent>(OnChartGeometryChanged);
            Description = descriptionLabel.text;
            ValueAxisLabel = axisLabel.text;
        }

        /// <summary>Replaces all bars. Every category must contain one value per series, in series order.</summary>
        public void SetData(IReadOnlyList<Series> series, IReadOnlyList<Category> categories)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));
            if (series.Any(item => item == null))
                throw new ArgumentException("Series cannot contain null entries.", nameof(series));
            foreach (var category in categories)
            {
                if (category == null || category.Values.Count != series.Count)
                    throw new ArgumentException("Each category must contain one value per series.", nameof(categories));
                if (category.Values.Any(value => value.Amount < 0))
                    throw new ArgumentException("This chart supports non-negative values only.", nameof(categories));
            }

            seriesData = series.ToArray();
            categoryData = categories.ToArray();
            Render();
        }

        private void Render()
        {
            if (double.IsNaN(MinimumStep) || double.IsInfinity(MinimumStep) || MinimumStep < 0)
                throw new ArgumentOutOfRangeException(nameof(MinimumStep));
            if (MinimumColumnCount < 0)
                throw new ArgumentOutOfRangeException(nameof(MinimumColumnCount));

            var stacks = categoryData.Select(category => (Category: category, Total: category.Values.Sum(value => value.Amount ?? 0m))).ToArray();
            EnableInClassList("bar-chart--horizontal", IsHorizontal);
            var container = IsHorizontal ? horizontalPlotArea : chartScrollView.contentContainer;
            if (columns.parent != container)
                container.Add(columns);

            plots.Clear();
            categoryLabels.Clear();
            columns.Clear();
            ticks.Clear();
            legend.Clear();
            valueAxis.style.marginTop = StyleKeyword.Null;
            valueAxis.style.marginRight = StyleKeyword.Null;
            valueAxis.style.marginLeft = StyleKeyword.Null;
            valueAxis.style.height = StyleKeyword.Null;
            ticks.style.width = StyleKeyword.Null;
            var hasData = categoryData.Any(category => category.Values.Any(value => value.Amount.HasValue));
            body.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            legend.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            emptyLabel.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;
            if (!hasData)
                return;

            var largestStack = (double)stacks.Max(stack => stack.Total);
            var step = Math.Max(CalculateStep(largestStack), MinimumStep);
            var maximum = step * IntervalCount;
            RenderAxis(step);
            foreach (var item in stacks)
                columns.Add(CreateBar(item.Category, item.Total, maximum));
            if (!IsHorizontal)
            {
                // Reserve the remaining positions without adding data, labels or tooltips.
                for (var i = stacks.Length; i < MinimumColumnCount; i++)
                {
                    var column = new VisualElement { pickingMode = PickingMode.Ignore };
                    column.AddToClassList("bar-chart__column");
                    var plot = new VisualElement { pickingMode = PickingMode.Ignore };
                    plot.AddToClassList("bar-chart__plot");
                    AddGridLines(plot);
                    plots.Add((plot, null, 0));
                    column.Add(plot);
                    columns.Add(column);
                }
            }
            RenderLegend();
        }

        private void RenderAxis(double step)
        {
            for (var i = 0; i <= IntervalCount; i++)
            {
                var tickValue = step * i;
                var tick = new Label(FormatAxisValue(tickValue)) { tooltip = tickValue.ToString(ValueFormat, NumberCulture) };
                tick.AddToClassList("bar-chart__tick");
                if (IsHorizontal)
                    tick.style.left = Length.Percent(100f * i / IntervalCount);
                else
                    tick.style.bottom = Length.Percent(100f * i / IntervalCount);
                ticks.Add(tick);
                tick.RegisterCallback<GeometryChangedEvent>(OnChartGeometryChanged);
            }
        }

        private VisualElement CreateBar(Category category, decimal total, double maximum)
        {
            var column = new VisualElement();
            column.AddToClassList("bar-chart__column");
            var plot = new VisualElement { pickingMode = PickingMode.Ignore };
            plot.AddToClassList("bar-chart__plot");
            AddGridLines(plot);
            AddSegments(plot, category, maximum);
            var totalAnchor = AddTotalLabel(plot, category, total, maximum);
            plots.Add((plot, totalAnchor, (double)total / maximum));
            plot.RegisterCallback<GeometryChangedEvent>(OnChartGeometryChanged);
            totalAnchor.RegisterCallback<GeometryChangedEvent>(OnChartGeometryChanged);
            column.Add(plot);
            var label = new Label(category.Label) { tooltip = category.Tooltip };
            label.AddToClassList("bar-chart__category-label");
            categoryLabels.Add(label);
            label.RegisterCallback<GeometryChangedEvent>(OnChartGeometryChanged);
            column.Add(label);
            return column;
        }

        private void AddGridLines(VisualElement plot)
        {
            for (var i = 0; i <= IntervalCount; i++)
            {
                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                line.AddToClassList("bar-chart__grid-line");
                // Keep the outer border inside the plot instead of beyond its clipping edge.
                if (IsHorizontal)
                {
                    if (i == IntervalCount)
                        line.style.right = 0;
                    else
                        line.style.left = Length.Percent(100f * i / IntervalCount);
                }
                else if (i == IntervalCount)
                    line.style.top = 0;
                else
                    line.style.bottom = Length.Percent(100f * i / IntervalCount);
                plot.Add(line);
            }
        }

        private void AddSegments(VisualElement plot, Category category, double maximum)
        {
            var stack = new VisualElement { pickingMode = PickingMode.Ignore };
            stack.AddToClassList("bar-chart__stack");
            for (var i = 0; i < seriesData.Length; i++)
            {
                var value = category.Values[i];
                if (!value.Amount.HasValue || value.Amount.Value == 0)
                    continue;

                var segment = new VisualElement { tooltip = value.Tooltip };
                segment.AddToClassList("bar-chart__segment");
                if (IsHorizontal)
                    segment.style.width = Length.Percent((float)((double)value.Amount.Value / maximum * 100));
                else
                    segment.style.height = Length.Percent((float)((double)value.Amount.Value / maximum * 100));
                segment.style.backgroundColor = seriesData[i].Color;
                stack.Add(segment);
            }
            plot.Add(stack);
        }

        private VisualElement AddTotalLabel(VisualElement plot, Category category, decimal total, double maximum)
        {
            var text = category.TotalLabel ?? (category.Values.All(value => value.Amount.HasValue) ? FormatValue(total) : "\u2014");
            var label = new Label(text) { tooltip = category.Tooltip };
            label.AddToClassList("bar-chart__total-label");
            var anchor = new VisualElement { pickingMode = PickingMode.Ignore };
            anchor.AddToClassList("bar-chart__total-anchor");
            if (IsHorizontal)
                anchor.style.left = Length.Percent((float)((double)total / maximum * 100));
            else
                anchor.style.bottom = Length.Percent((float)((double)total / maximum * 100));
            anchor.Add(label);
            plot.Add(anchor);
            return anchor;
        }

        private void OnChartGeometryChanged(GeometryChangedEvent evt)
        {
            if (layoutUpdatePending)
                return;

            layoutUpdatePending = true;
            schedule.Execute(UpdateChartLayout);
        }

        private void UpdateChartLayout()
        {
            layoutUpdatePending = false;
            if (plots.Count == 0)
                return;
            if (UpdateLabelWidth())
                return;

            var requiredMargin = IsHorizontal && ticks.childCount > 0 ? ticks[ticks.childCount - 1].resolvedStyle.width / 2 : 0;
            if (float.IsNaN(requiredMargin))
                return;
            foreach (var item in plots)
            {
                if (item.Anchor == null)
                    continue;

                var size = IsHorizontal ? item.Plot.resolvedStyle.width : item.Plot.resolvedStyle.height;
                var labelSize = IsHorizontal ? item.Anchor.resolvedStyle.width + item.Anchor.resolvedStyle.marginLeft
                    : item.Anchor.resolvedStyle.height + item.Anchor.resolvedStyle.marginBottom;
                if (float.IsNaN(size) || float.IsNaN(labelSize) || size <= 0)
                    return;

                if (IsHorizontal)
                    requiredMargin = Mathf.Max(requiredMargin, labelSize - size * (float)(1 - item.Fraction));
                else
                {
                    var totalHeight = size + item.Plot.resolvedStyle.marginTop;
                    if (item.Fraction > 0)
                        requiredMargin = Mathf.Max(requiredMargin, (labelSize - totalHeight * (float)(1 - item.Fraction)) / (float)item.Fraction);
                }
            }

            foreach (var item in plots)
            {
                if (IsHorizontal)
                {
                    if (!Mathf.Approximately(item.Plot.resolvedStyle.marginRight, requiredMargin))
                        item.Plot.style.marginRight = requiredMargin;
                }
                else
                {
                    var totalHeight = item.Plot.resolvedStyle.height + item.Plot.resolvedStyle.marginTop;
                    if (!Mathf.Approximately(item.Plot.resolvedStyle.marginTop, requiredMargin))
                        item.Plot.style.marginTop = requiredMargin;
                    var height = Mathf.Max(1, totalHeight - requiredMargin);
                    if (!Mathf.Approximately(item.Plot.resolvedStyle.height, height))
                        item.Plot.style.height = height;
                }
            }

            if (IsHorizontal)
            {
                if (!Mathf.Approximately(valueAxis.resolvedStyle.marginRight, requiredMargin))
                    valueAxis.style.marginRight = requiredMargin;
            }
            else
            {
                if (!Mathf.Approximately(valueAxis.resolvedStyle.marginTop, requiredMargin))
                    valueAxis.style.marginTop = requiredMargin;
                if (!Mathf.Approximately(valueAxis.resolvedStyle.height, plots[0].Plot.resolvedStyle.height))
                    valueAxis.style.height = plots[0].Plot.resolvedStyle.height;
            }
        }

        private bool UpdateLabelWidth()
        {
            var labels = IsHorizontal ? categoryLabels : ticks.Children().OfType<Label>().ToList();
            if (labels.Count == 0)
                return false;

            var width = labels.Max(label => label.MeasureTextSize(label.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x
                + label.resolvedStyle.paddingLeft + label.resolvedStyle.paddingRight);
            if (float.IsNaN(width) || width <= 0)
                return false;

            width = Mathf.Ceil(width);
            var changed = false;
            if (IsHorizontal)
            {
                foreach (var label in labels)
                {
                    if (Mathf.Approximately(label.style.width.value.value, width))
                        continue;

                    label.style.width = width;
                    changed = true;
                }

                // Use the actual width after USS limits it to the available panel space.
                var gutter = labels[0].resolvedStyle.width;
                if (!float.IsNaN(gutter) && gutter > 0 && !Mathf.Approximately(valueAxis.resolvedStyle.marginLeft, gutter))
                {
                    valueAxis.style.marginLeft = gutter;
                    changed = true;
                }
            }
            else
            {
                if (!Mathf.Approximately(ticks.style.width.value.value, width))
                {
                    ticks.style.width = width;
                    changed = true;
                }
                for (var i = 0; i < categoryLabels.Count; i++)
                {
                    var category = categoryLabels[i];
                    var total = plots[i].Anchor.Q<Label>();
                    var categoryWidth = category.MeasureTextSize(category.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
                    var totalWidth = total.MeasureTextSize(total.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x
                        + total.resolvedStyle.paddingLeft + total.resolvedStyle.paddingRight + total.resolvedStyle.borderLeftWidth + total.resolvedStyle.borderRightWidth;
                    var columnWidth = Mathf.Ceil(Mathf.Max(categoryWidth, totalWidth) + 4);
                    if (!float.IsNaN(columnWidth) && !Mathf.Approximately(category.parent.style.minWidth.value.value, columnWidth))
                    {
                        category.parent.style.minWidth = columnWidth;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private void RenderLegend()
        {
            foreach (var item in seriesData)
                ChartUtility.AddLegendEntry(legend, "bar-chart", item.Label, item.Color);
        }

        /// <summary>Formats a value for totals and axes. Tooltips can retain the original, unabridged value.</summary>
        public string FormatValue(decimal value) => ChartUtility.FormatValue(value, NumberCulture, ValueFormat, CompactNumbers, ThousandSuffix, MillionSuffix, BillionSuffix);

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
            var multiplier = StepMultipliers.First(value => value >= fraction);
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
