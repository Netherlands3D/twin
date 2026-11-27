using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DashboardAnnotation : VisualElement
    {
        private readonly VisualElement changes;
        private readonly Header header;
        private readonly Label count;
        private readonly Label empty;
        private readonly UnityEngine.UIElements.ScrollView scroll;
        private IReadOnlyDictionary<string, Color> colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        private DashboardObjectData.BlockData data;

        public DashboardAnnotation()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            changes = this.Q("AnnotationChanges");
            header = this.Q<Header>("AnnotationHeader");
            count = this.Q<Label>("AnnotationCount");
            empty = this.Q<Label>("AnnotationEmpty");
            scroll = this.Q<UnityEngine.UIElements.ScrollView>("AnnotationScroll");
        }

        internal void SetData(DashboardObjectData.BlockData block)
        {
            if (ReferenceEquals(data, block))
                return;

            data = block;
            changes.Clear();
            EnableInClassList(UtilityClassConstants.HIDDEN, block == null);
            if (block == null)
                return;

            header.LabelText = DashboardSummary.BlockName(block.Block);
            count.text = block.ProgramChanges.Count == 1 ? "1 programmawijziging" : $"{block.ProgramChanges.Count} programmawijzigingen";
            foreach (var change in DashboardData.Phases.Reverse().SelectMany(phase => block.ProgramChanges.Where(change => string.Equals(change.Phase, phase, StringComparison.OrdinalIgnoreCase))))
            {
                var entry = new VisualElement();
                entry.AddToClassList("dashboard-annotation__change");
                var labels = new VisualElement();
                labels.AddToClassList("dashboard-annotation__labels");
                var phase = new Label(change.Phase);
                phase.AddToClassList("chart-badge");
                phase.AddToClassList("dashboard-annotation__phase");
                labels.Add(phase);
                if (!string.IsNullOrWhiteSpace(change.Program))
                {
                    var program = new Label(change.Program) { enableRichText = false };
                    program.AddToClassList("ring-chart__total-label");
                    labels.Add(program);
                }
                entry.Add(labels);
                var text = new Label(change.Deviation) { enableRichText = false };
                text.AddToClassList("dashboard-annotation__text");
                entry.Add(text);
                changes.Add(entry);
            }
            empty.EnableInClassList(UtilityClassConstants.HIDDEN, block.ProgramChanges.Count > 0);
            scroll.scrollOffset = Vector2.zero;
            RefreshColors();
        }

        internal void SetPosition(Vector2 panelPosition, bool visible)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !visible || data == null);
            if (!visible || data == null)
                return;

            var position = parent.WorldToLocal(panelPosition);
            style.left = position.x;
            style.top = position.y;
        }

        internal void SetPhaseColors(IReadOnlyDictionary<string, Color> phaseColors)
        {
            colors = phaseColors;
            RefreshColors();
        }

        private void RefreshColors()
        {
            foreach (var phase in changes.Query<Label>(className: "dashboard-annotation__phase").ToList())
                phase.style.backgroundColor = DashboardColors.GetPhaseColor(phase.text, colors);
        }
    }
}
