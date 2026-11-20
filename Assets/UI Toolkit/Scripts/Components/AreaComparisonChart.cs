using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    /// <summary>Compares non-negative values using circle areas on one shared scale.</summary>
    [UxmlElement]
    public partial class AreaComparisonChart : VisualElement
    {
        private readonly Header titleHeader;
        private readonly VisualElement items;
        private readonly VisualElement legend;
        private readonly Label emptyLabel;

        [UxmlAttribute("title")]
        public string Title { get => titleHeader.LabelText; set => titleHeader.LabelText = value; }

        [UxmlAttribute("icon")]
        public string Icon { get => titleHeader.Image; set => titleHeader.Image = value; }

        [UxmlAttribute("empty-text")]
        public string EmptyText { get => emptyLabel.text; set => emptyLabel.text = value; }

        /// <summary>Used for values without caller-supplied display text on the next SetData call.</summary>
        public CultureInfo NumberCulture { get; set; } = CultureInfo.CurrentCulture;
        public string ValueFormat { get; set; } = "#,0.##";

        public AreaComparisonChart()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            titleHeader = this.Q<Header>("ChartTitle");
            items = this.Q("ChartItems");
            legend = this.Q("ChartLegend");
            emptyLabel = this.Q<Label>("EmptyChart");
        }

        /// <summary>
        /// Replaces the items. A positive reference value fixes the scale and must cover every amount.
        /// Null and zero amounts have no circle; their labels remain visible. Callers supply units and completeness markers.
        /// </summary>
        public void SetData(IReadOnlyList<Item> data, decimal? referenceValue = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Any(item => item == null || item.Amount < 0))
                throw new ArgumentException("Items must be non-null and have non-negative amounts.", nameof(data));

            var largest = data.Count == 0 ? 0m : data.Max(item => item.Amount ?? 0m);
            if (referenceValue.HasValue && (referenceValue.Value <= 0 || referenceValue.Value < largest))
                throw new ArgumentOutOfRangeException(nameof(referenceValue), "The reference must be positive and at least as large as every amount.");
            var scale = referenceValue ?? (largest > 0 ? largest : 1m);

            items.Clear();
            legend.Clear();
            items.style.display = data.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            legend.style.display = data.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            emptyLabel.style.display = data.Count > 0 ? DisplayStyle.None : DisplayStyle.Flex;
            foreach (var item in data)
            {
                var entry = new VisualElement();
                entry.AddToClassList("area-comparison-chart__item");
                var value = new Label(item.ValueText ?? item.Amount?.ToString(ValueFormat, NumberCulture) ?? "\u2014") { tooltip = item.Tooltip };
                value.AddToClassList("area-comparison-chart__value-label");
                value.AddToClassList("chart-badge");
                value.style.backgroundColor = item.Color;
                entry.Add(value);
                var diameterFraction = item.Amount > 0 ? (float)Math.Sqrt((double)(item.Amount.Value / scale)) : 0;
                var canvas = new VisualElement { pickingMode = PickingMode.Ignore };
                canvas.AddToClassList("area-comparison-chart__canvas");
                canvas.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    if (evt.oldRect.size == evt.newRect.size)
                        return;

                    // Move the label through the unused space above a smaller, bottom-aligned circle.
                    var offset = evt.newRect.height - Mathf.Min(evt.newRect.width, evt.newRect.height) * diameterFraction;
                    value.style.translate = new Translate(0, offset);
                });
                if (item.Amount > 0)
                {
                    // Area follows the amount; scaling the diameter directly would exaggerate differences.
                    var diameter = diameterFraction * 100;
                    var circle = new AreaCircle { tooltip = item.Tooltip };
                    circle.AddToClassList("area-comparison-chart__circle");
                    circle.style.width = Length.Percent(diameter);
                    circle.style.height = Length.Percent(diameter);
                    circle.style.color = item.Color;
                    canvas.Add(circle);
                }
                entry.Add(canvas);
                items.Add(entry);

                var legendEntry = new VisualElement { tooltip = item.Tooltip };
                legendEntry.AddToClassList("area-comparison-chart__legend-entry");
                var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
                swatch.AddToClassList("area-comparison-chart__swatch");
                swatch.style.backgroundColor = item.Color;
                legendEntry.Add(swatch);
                legendEntry.Add(new Label(item.Label));
                legend.Add(legendEntry);
            }
        }

        public sealed class Item
        {
            public string Label { get; }
            public decimal? Amount { get; }
            public Color Color { get; }
            public string Tooltip { get; }
            public string ValueText { get; }

            public Item(string label, decimal? amount, Color color, string tooltip = null, string valueText = null)
            {
                Label = label;
                Amount = amount;
                Color = color;
                Tooltip = tooltip;
                ValueText = valueText;
            }
        }

        private sealed class AreaCircle : VisualElement
        {
            public AreaCircle()
            {
                generateVisualContent += Draw;
            }

            public override bool ContainsPoint(Vector2 localPoint)
            {
                var radius = Mathf.Min(contentRect.width, contentRect.height) / 2;
                var centre = new Vector2(contentRect.center.x, contentRect.yMax - radius);
                return radius > 0 && (localPoint - centre).sqrMagnitude <= radius * radius;
            }

            private void Draw(MeshGenerationContext context)
            {
                var radius = Mathf.Min(contentRect.width, contentRect.height) / 2;
                if (radius <= 0)
                    return;

                var painter = context.painter2D;
                painter.fillColor = resolvedStyle.color;
                painter.BeginPath();
                painter.Arc(new Vector2(contentRect.center.x, contentRect.yMax - radius), radius, Angle.Degrees(0), Angle.Degrees(360));
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
