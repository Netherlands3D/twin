using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>Displays non-negative shares. Callers supply colours, units and any completeness markers.</summary>
    [UxmlElement]
    public partial class RingChart : VisualElement
    {
        private static readonly CustomStyleProperty<float> HorizontalInset = new("--ring-chart-horizontal-inset");
        private static readonly CustomStyleProperty<float> VerticalInset = new("--ring-chart-vertical-inset");
        private static readonly CustomStyleProperty<float> LabelGap = new("--ring-chart-label-gap");
        private readonly Header titleHeader;
        private readonly VisualElement plot;
        private readonly VisualElement segments;
        private readonly VisualElement percentageLabels;
        private readonly VisualElement legend;
        private readonly Label totalLabel;
        private readonly Label unitLabel;
        private readonly Label emptyLabel;

        [UxmlAttribute("title")]
        public string Title { get => titleHeader.LabelText; set => titleHeader.LabelText = value; }

        [UxmlAttribute("icon")]
        public string Icon { get => titleHeader.Image; set => titleHeader.Image = value; }

        [UxmlAttribute("unit")]
        public string Unit
        {
            get => unitLabel.text;
            set
            {
                unitLabel.text = value;
                unitLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("empty-text")]
        public string EmptyText { get => emptyLabel.text; set => emptyLabel.text = value; }

        public CultureInfo NumberCulture { get; set; } = CultureInfo.CurrentCulture;

        public RingChart()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            titleHeader = this.Q<Header>("ChartTitle");
            plot = this.Q("ChartPlot");
            segments = this.Q("ChartSegments");
            percentageLabels = this.Q("ChartPercentages");
            legend = this.Q("ChartLegend");
            totalLabel = this.Q<Label>("ChartTotal");
            unitLabel = this.Q<Label>("ChartUnit");
            emptyLabel = this.Q<Label>("EmptyChart");
            this.Q(className: "ring-chart__track").Add(new RingSegment(0, 360) { pickingMode = PickingMode.Ignore });
            plot.RegisterCallback<GeometryChangedEvent>(OnTotalGeometryChanged);
            plot.RegisterCallback<CustomStyleResolvedEvent>(_ => UpdateTotalPosition());
            totalLabel.parent.RegisterCallback<GeometryChangedEvent>(OnTotalGeometryChanged);
        }

        private static float GetStyleLength(VisualElement element, CustomStyleProperty<float> property, float fallback) =>
            element.customStyle.TryGetValue(property, out var value) ? Mathf.Max(0, value) : fallback;

        private static float GetOuterRadius(VisualElement element) => Mathf.Max(0, Mathf.Min(
            element.contentRect.width / 2 - GetStyleLength(element, HorizontalInset, 72),
            element.contentRect.height / 2 - GetStyleLength(element, VerticalInset, 40)));

        private void OnTotalGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size != evt.newRect.size)
                UpdateTotalPosition();
        }

        private void UpdateTotalPosition()
        {
            var height = totalLabel.parent.resolvedStyle.height;
            if (float.IsNaN(height))
                return;

            var top = plot.contentRect.center.y - GetOuterRadius(plot) - GetStyleLength(plot, LabelGap, 8) - height;
            if (!Mathf.Approximately(totalLabel.parent.style.top.value.value, top))
                totalLabel.parent.style.top = top;
        }

        /// <summary>Null amounts have no arc. An optional total text can describe a known subtotal.</summary>
        public void SetData(IReadOnlyList<Segment> data, string totalText = null, string totalTooltip = null, bool isIncomplete = false)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Any(segment => segment == null || segment.Amount < 0))
                throw new ArgumentException("Segments must be non-null and have non-negative amounts.", nameof(data));

            var total = data.Sum(segment => segment.Amount ?? 0m);
            segments.Clear();
            percentageLabels.Clear();
            legend.Clear();
            var hasData = data.Count > 0;
            plot.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            legend.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            totalLabel.parent.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
            emptyLabel.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;
            totalLabel.text = totalText ?? (data.All(segment => segment.Amount.HasValue) ? total.ToString("#,0.##", NumberCulture) : "\u2014");
            totalLabel.tooltip = totalTooltip;

            var fraction = 0m;
            foreach (var segment in data)
            {
                if (total > 0 && segment.Amount > 0)
                {
                    var share = segment.Amount.Value / total;
                    var next = fraction + share;
                    var percentage = share.ToString("0.#%", NumberCulture) + (isIncomplete ? "*" : "");
                    var tooltip = string.IsNullOrEmpty(segment.Tooltip) ? percentage : $"{segment.Tooltip}\n{percentage}";
                    var label = new Label(percentage) { tooltip = tooltip };
                    label.AddToClassList("ring-chart__percentage");
                    label.AddToClassList("chart-badge");
                    label.style.backgroundColor = segment.Color;
                    percentageLabels.Add(label);
                    var arc = new RingSegment((float)(fraction * 360m), (float)(next * 360m), label)
                    {
                        tooltip = tooltip
                    };
                    arc.style.color = segment.Color;
                    segments.Add(arc);
                    fraction = next;
                }

                var entry = new VisualElement { tooltip = segment.Tooltip };
                entry.AddToClassList("ring-chart__legend-entry");
                var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
                swatch.AddToClassList("ring-chart__swatch");
                swatch.style.backgroundColor = segment.Color;
                entry.Add(swatch);
                entry.Add(new Label(segment.Label));
                legend.Add(entry);
            }
        }

        public sealed class Segment
        {
            public string Label { get; }
            public decimal? Amount { get; }
            public Color Color { get; }
            public string Tooltip { get; }

            public Segment(string label, decimal? amount, Color color, string tooltip = null)
            {
                Label = label;
                Amount = amount;
                Color = color;
                Tooltip = tooltip;
            }
        }

        // Individual hit regions let the existing runtime tooltip behaviour handle each arc.
        private sealed class RingSegment : VisualElement
        {
            private static readonly CustomStyleProperty<float> Thickness = new("--ring-chart-thickness");
            private readonly float startAngle;
            private readonly float endAngle;
            private readonly Label percentageLabel;

            public RingSegment(float startAngle, float endAngle, Label percentageLabel = null)
            {
                this.startAngle = startAngle;
                this.endAngle = endAngle;
                AddToClassList("ring-chart__segment");
                generateVisualContent += Draw;
                RegisterCallback<CustomStyleResolvedEvent>(_ =>
                {
                    MarkDirtyRepaint();
                    if (this.percentageLabel != null)
                        UpdatePercentagePosition();
                });
                this.percentageLabel = percentageLabel;
                if (percentageLabel != null)
                {
                    RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    percentageLabel.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
                    RegisterCallback<PointerEnterEvent>(_ => SetHighlighted(true));
                    RegisterCallback<PointerLeaveEvent>(_ => SetHighlighted(false));
                    percentageLabel.RegisterCallback<PointerEnterEvent>(_ => SetHighlighted(true));
                    percentageLabel.RegisterCallback<PointerLeaveEvent>(_ => SetHighlighted(false));
                }
            }

            private void OnGeometryChanged(GeometryChangedEvent evt)
            {
                if (evt.oldRect.size != evt.newRect.size)
                    UpdatePercentagePosition();
            }

            private void SetHighlighted(bool highlighted)
            {
                EnableInClassList("ring-chart__highlighted", highlighted);
                percentageLabel.EnableInClassList("ring-chart__highlighted", highlighted);
                if (highlighted)
                    percentageLabel.BringToFront();
            }

            private void UpdatePercentagePosition()
            {
                var (outer, _) = GetRadii();
                var angle = (startAngle + endAngle) / 2 * Mathf.Deg2Rad - Mathf.PI / 2;
                var horizontal = Mathf.Cos(angle);
                var vertical = Mathf.Sin(angle);
                var size = percentageLabel.layout.size;
                if (float.IsNaN(size.x) || float.IsNaN(size.y) || size.x <= 0 || size.y <= 0)
                    return;

                // Include the badge dimensions so every side clears the ring by the same radial gap.
                var radius = outer + GetStyleLength(this, LabelGap, 8) + (Mathf.Abs(horizontal) * size.x + Mathf.Abs(vertical) * size.y) / 2;
                var left = contentRect.center.x + horizontal * radius;
                var top = contentRect.center.y + vertical * radius;
                if (!Mathf.Approximately(percentageLabel.style.left.value.value, left))
                    percentageLabel.style.left = left;
                if (!Mathf.Approximately(percentageLabel.style.top.value.value, top))
                    percentageLabel.style.top = top;
            }

            private (float Outer, float Width) GetRadii()
            {
                // Reserve space around the ring for badges, keeping them inside the panel.
                var outer = GetOuterRadius(this);
                var width = customStyle.TryGetValue(Thickness, out var thickness) ? thickness : 32f;
                return (outer, Mathf.Clamp(width, 0, outer));
            }

            public override bool ContainsPoint(Vector2 localPoint)
            {
                var (outer, width) = GetRadii();
                var offset = localPoint - contentRect.center;
                if (outer <= 0 || offset.magnitude < outer - width || offset.magnitude > outer)
                    return false;

                var angle = Mathf.Repeat(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg + 90, 360);
                return angle >= startAngle && angle < endAngle;
            }

            private void Draw(MeshGenerationContext context)
            {
                var (outer, width) = GetRadii();
                if (outer <= 0 || width <= 0)
                    return;

                var painter = context.painter2D;
                painter.strokeColor = resolvedStyle.color;
                painter.lineWidth = width;
                painter.BeginPath();
                painter.Arc(contentRect.center, outer - width / 2, Angle.Degrees(startAngle - 90), Angle.Degrees(endAngle - 90));
                painter.Stroke();
            }
        }
    }
}
