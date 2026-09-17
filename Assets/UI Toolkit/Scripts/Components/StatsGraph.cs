using System;
using System.Collections.Generic;
using System.Globalization;
using Netherlands3D.Twin.Services;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    
    [UxmlElement]
    public partial class StatsGraph : VisualElement
    {
        private Label titleLabel;
        private Label valueLabel;
        private VisualElement graphElement;

        private DebugStat source;
        private bool updatePending;
        private bool interpolateDisplayedValues = false;
        
        private string title = "Value:";

        private readonly List<DebugStat.TimedValue> labelTimedValues = new();
        private readonly List<DebugStat.TimedValue> graphTimedValues = new();
        
        [UxmlAttribute("title")]
        public string Title
        {
            get => title;
            set
            {
                title = value;
                titleLabel.text = value;
            }
        }

        [UxmlAttribute("graph-duration")]
        public float GraphDuration { get; set; } = 60f;

        private const double ValueLabelUpdateTime = .25d;
        private const double GraphElementUpdateTime = .1d;

        public StatsGraph()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            titleLabel = this.Q<Label>("Title");
            valueLabel = this.Q<Label>("Value");
            graphElement = this.Q<VisualElement>("Graph");
            graphElement.generateVisualContent += DrawGraph;

            schedule.Execute(UpdateValueLabel).Every((long)(ValueLabelUpdateTime * 1000));
            schedule.Execute(UpdateGraph).Every((long)(GraphElementUpdateTime * 1000));
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public void Bind(DebugStat newSource)
        {
            source = newSource;
            if (panel != null)
            {
                Subscribe();
            }
        }

        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            Subscribe();
            Reset();
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            Unsubscribe();
        }
        
        private void Subscribe()
        {
            if (source != null)
                source.Updated += OnSourceUpdated;
        }

        private void Unsubscribe()
        {
            if (source != null)
                source.Updated -= OnSourceUpdated;
        }
        
        private void OnSourceUpdated(DebugStat _)
        {
            updatePending = true;
        }

        private void UpdateValueLabel()
        {
            if (!updatePending || source == null || !source.HasValues)
                return;

            source.GetValues(labelTimedValues);
            valueLabel.text = labelTimedValues[^1].Value.ToString("F1",  CultureInfo.CurrentCulture);
        }

        private void UpdateGraph()
        {
            if (!updatePending || source == null || !source.HasValues)
                return;
            
            source.GetValuesInRangeRelative(-GraphDuration, 0, graphTimedValues);
            if (graphTimedValues.Count == 0)
            {
                updatePending = false;
                return;
            }

            var summary = graphTimedValues.Summarize();

            var targetMaximumDisplayedValue = summary.Average + summary.Maximum;
            var interpolation = 1f - Math.Pow(0.5d, GraphElementUpdateTime / 1d);
            if (!interpolateDisplayedValues)
            {
                maximumDisplayedValue = targetMaximumDisplayedValue;
                interpolateDisplayedValues = true;
            }
            else
            {
                maximumDisplayedValue += (targetMaximumDisplayedValue - maximumDisplayedValue) * interpolation; //lerp
            }

            graphElement.MarkDirtyRepaint();
        }

        private double minimumDisplayValue = 0d;
        private double maximumDisplayedValue = 300d;

        private void DrawGraph(MeshGenerationContext context)
        {
            if (graphTimedValues.Count <= 0)
                return;
            
            var bounds = graphElement.contentRect;
            var painter = context.painter2D;

            painter.strokeColor = new Color(0.3f, 0.9f, 0.6f);
            painter.lineWidth = 1f;

            painter.BeginPath();
            painter.MoveTo(GraphValueToGraphPosition(graphTimedValues[0]));
            foreach (var t in graphTimedValues)
            {
                painter.LineTo(GraphValueToGraphPosition(t));
            }
            painter.Stroke();
            return;

            Vector2 GraphValueToGraphPosition(DebugStat.TimedValue timedValue)
            {
                var howLongAgo = (float)(timedValue.Timestamp - Time.unscaledTimeAsDouble);
                var x = bounds.xMax + howLongAgo * bounds.width / GraphDuration;

                var y = (float)(bounds.yMax - ((float)timedValue.Value - minimumDisplayValue) * bounds.height / (maximumDisplayedValue - minimumDisplayValue));
                
                return new Vector2(x, y);
            }
            
        }

        private void Reset()
        {
            valueLabel.text = "-";
            updatePending = source != null;
            interpolateDisplayedValues = false;
        }
        
    }
}
