using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Tiles3D
{
    /// <summary>
    /// UI Toolkit element that visualises Unity memory counters over time.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class MemoryStatsGraph : VisualElement
    {
        const float MinSampleInterval = 0.1f;

        public new class UxmlFactory : UxmlFactory<MemoryStatsGraph, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlFloatAttributeDescription sampleIntervalAttr = new UxmlFloatAttributeDescription
            {
                name = "sample-interval",
                defaultValue = 0.5f
            };

            readonly UxmlIntAttributeDescription maxSamplesAttr = new UxmlIntAttributeDescription
            {
                name = "max-samples",
                defaultValue = 0
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                if (ve is MemoryStatsGraph graph)
                {
                    graph.SampleInterval = Mathf.Max(MinSampleInterval, sampleIntervalAttr.GetValueFromBag(bag, cc));
                    graph.MaxSamples = Mathf.Max(0, maxSamplesAttr.GetValueFromBag(bag, cc));
                }
            }
        }

        readonly List<float> heapSizeSamples = new List<float>();
        readonly List<float> gcHeapSamples = new List<float>();
        readonly Label hardLimitLabel;
        readonly float hardLimitMB = 2048f;
        float referenceMB = 1f;

        IVisualElementScheduledItem sampleScheduler;
        float sampleInterval = 0.5f;
        int maxSamples;

        public float LatestHeapSizeMB { get; private set; }
        public float LatestGcHeapMB { get; private set; }

        public event Action<MemoryStatsGraph> SamplesUpdated;

        public float SampleInterval
        {
            get => sampleInterval;
            set
            {
                float clamped = Mathf.Max(MinSampleInterval, value);
                if (!Mathf.Approximately(sampleInterval, clamped))
                {
                    sampleInterval = clamped;
                    RestartSampling();
                }
            }
        }

        public int MaxSamples
        {
            get => maxSamples;
            set
            {
                int clamped = Mathf.Max(0, value);
                if (maxSamples != clamped)
                {
                    maxSamples = clamped;
                    MarkDirtyRepaint();
                }
            }
        }

        public MemoryStatsGraph()
        {
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Ignore;
            style.flexGrow = 1f;
            RegisterCallback<AttachToPanelEvent>(HandleAttach);
            RegisterCallback<DetachFromPanelEvent>(HandleDetach);

            hardLimitLabel = new Label();
            hardLimitLabel.style.position = Position.Absolute;
            hardLimitLabel.style.left = 8f;
            hardLimitLabel.style.color = new Color(1f, 0.5f, 0.5f, 0.9f);
            hardLimitLabel.style.fontSize = 12f;
            hardLimitLabel.text = $"{FormatMemory(hardLimitMB)} limit";
            hardLimitLabel.visible = hardLimitMB > 0f;
            hierarchy.Add(hardLimitLabel);
        }

        void HandleAttach(AttachToPanelEvent evt) => RestartSampling();

        void HandleDetach(DetachFromPanelEvent evt)
        {
            sampleScheduler?.Pause();
            sampleScheduler = null;
        }


        void RestartSampling()
        {
            if (panel == null)
            {
                return;
            }

            sampleScheduler?.Pause();
            sampleScheduler = schedule.Execute(() => Sample()).Every(Mathf.RoundToInt(SampleInterval * 1000f));
            Sample();
        }

        void Sample()
        {
            LatestHeapSizeMB = SystemInfo.systemMemorySize;
            LatestGcHeapMB = BytesToMegabytes(System.GC.GetTotalMemory(false));

            AddSample(heapSizeSamples, LatestHeapSizeMB);
            AddSample(gcHeapSamples, LatestGcHeapMB);

            float dataMax = Mathf.Max(
                FindMax(heapSizeSamples),
                FindMax(gcHeapSamples),
                1f);

            if (hardLimitMB > 0f)
            {
                referenceMB = hardLimitMB;
            }
            else
            {
                referenceMB = Mathf.Max(dataMax, 1f);
            }

            MarkDirtyRepaint();
            SamplesUpdated?.Invoke(this);
        }

        void AddSample(List<float> samples, float value)
        {
            samples.Add(value);
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect rect = contentRect;
            if (rect.width < 2f || rect.height < 2f)
            {
                return;
            }

            var painter = ctx.painter2D;
            painter.lineWidth = 1f;

            painter.fillColor = new Color(0f, 0f, 0f, 0.35f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill();

            painter.strokeColor = new Color(1f, 1f, 1f, 0.35f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMax));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.Stroke();

            float reference = Mathf.Max(referenceMB, 1f);

            DrawSeries(painter, rect, heapSizeSamples, new Color32(255, 220, 64, 255), reference);
            DrawSeries(painter, rect, gcHeapSamples, new Color32(120, 255, 120, 255), reference);

            DrawHardLimit(painter, rect, reference);
        }

        void DrawSeries(Painter2D painter, Rect area, List<float> samples, Color color, float reference)
        {
            int count = samples.Count;
            if (count < 2)
            {
                return;
            }

            int widthSamples = Mathf.Max(2, Mathf.Min(count, Mathf.RoundToInt(area.width)));
            int displaySamples = widthSamples;
            if (MaxSamples > 0)
            {
                displaySamples = Mathf.Min(displaySamples, MaxSamples);
            }
            displaySamples = Mathf.Clamp(displaySamples, 2, count);

            float stepX = area.width / (displaySamples - 1);
            float compressionRatio = (count - 1f) / (displaySamples - 1f);

            painter.strokeColor = color;
            painter.BeginPath();

            for (int i = 0; i < displaySamples; i++)
            {
                float sampleIndex = i * compressionRatio;
                int index = Mathf.Clamp(Mathf.RoundToInt(sampleIndex), 0, count - 1);
                float value = Mathf.Clamp(samples[index], 0f, reference);
                float x = area.xMin + i * stepX;
                float y = Mathf.Lerp(area.yMax, area.yMin, value / reference);

                if (i == 0)
                {
                    painter.MoveTo(new Vector2(x, y));
                }
                else
                {
                    painter.LineTo(new Vector2(x, y));
                }
            }

            painter.Stroke();
        }

        static float FindMax(List<float> samples)
        {
            float max = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                if (samples[i] > max)
                {
                    max = samples[i];
                }
            }
            return max;
        }

        void DrawHardLimit(Painter2D painter, Rect rect, float reference)
        {
            if (hardLimitMB <= 0f)
            {
                hardLimitLabel.visible = false;
                return;
            }
            hardLimitLabel.visible = true;

            float clampedRef = Mathf.Max(reference, hardLimitMB, 1f);
            float t = Mathf.Clamp01(hardLimitMB / clampedRef);
            float y = Mathf.Lerp(rect.yMax, rect.yMin, t);

            painter.strokeColor = new Color(1f, 0.2f, 0.2f, 0.6f);
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, y));
            painter.LineTo(new Vector2(rect.xMax, y));
            painter.Stroke();

            hardLimitLabel.text = $"{FormatMemory(hardLimitMB)} limit";
            hardLimitLabel.style.top = y - 26f;
        }

        static float BytesToMegabytes(long bytes) => bytes / (1024f * 1024f);
        static string FormatMemory(float megabytes)
        {
            if (megabytes >= 1024f)
            {
                return $"{megabytes / 1024f:0.0} GB";
            }

            return $"{megabytes:0.0} MB";
        }
    }
}
