using System;
using System.Globalization;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using UnityEngine;

namespace Netherlands3D
{
    [RequireComponent(typeof(LayerGameObject))]
    public class TimelineGeoJson : MonoBehaviour
    {
        private GeoJsonLayerGameObject visualization;
        private TimelineLayerPropertyData timelineLayerPropertyData;
        private ColorPropertyData stylingPropertyData;
        private SunTime sunTime;

        void Start()
        {
            visualization = GetComponent<GeoJsonLayerGameObject>();
            visualization.InitProperty<TimelineLayerPropertyData>(visualization.LayerData.LayerProperties);
            timelineLayerPropertyData = visualization.LayerData.GetProperty<TimelineLayerPropertyData>();

            stylingPropertyData = visualization.LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();

            if (stylingPropertyData == null) return;

            sunTime = ServiceLocator.GetService<SunTime>();
            sunTime?.timeOfDayChanged.AddListener(OnTimeChanged);
        }

        private void OnDestroy()
        {
            sunTime?.timeOfDayChanged.RemoveListener(OnTimeChanged);
        }

        private void OnTimeChanged(DateTime currentTime)
        {
            // var currentState = GetBuildState(currentTime);
            // SetVisibility(currentState == BuildState.Normal);

            stylingPropertyData = visualization.LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();
            if (stylingPropertyData == null || !TryGetColorForFeature(currentTime, out Color color))
                return;

            stylingPropertyData.ColorType = Symbolizer.StrokeColorProperty;
            stylingPropertyData.SetDefaultSymbolizerColor(color);

            // visualization.LineRenderer3D.SetAllColors(color);
        }

        // private void SetVisibility(bool visible)
        // {
        //     visualization.LineRenderer3D.enabled = visible;
        // }

        private BuildState GetBuildState(DateTime currentTime)
        {
            var state = BuildState.Normal;
            if (timelineLayerPropertyData.BuildStart.HasValue && currentTime < timelineLayerPropertyData.BuildStart.Value)
                state = BuildState.PreBuild;
            else if (timelineLayerPropertyData.BuildEnd.HasValue && currentTime < timelineLayerPropertyData.BuildEnd.Value)
                state = BuildState.Building;
            else if (timelineLayerPropertyData.DemolishEnd.HasValue && currentTime > timelineLayerPropertyData.DemolishEnd.Value)
                state = BuildState.PostDemolish;
            else if (timelineLayerPropertyData.DemolishStart.HasValue && currentTime > timelineLayerPropertyData.DemolishStart.Value)
                state = BuildState.Demolishing;

            return state;
        }

        private bool TryGetColorForFeature(DateTime currentTime, out Color color)
        {
            color = default;
            foreach (var feature in visualization.GeoJsonFeatures)
            {
                if (feature?.Properties == null
                    || !feature.Properties.TryGetValue("timestamp", out object timestampValue)
                    || !feature.Properties.TryGetValue("count", out object countValue)
                    || timestampValue == null
                    || countValue == null)
                    continue;

                if (!DateTime.TryParse(timestampValue.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime dateTime))
                    continue;

                string countText = countValue.ToString();
                if (!float.TryParse(countText, NumberStyles.Float, CultureInfo.InvariantCulture, out float count)
                    && !float.TryParse(countText, out count))
                    continue;

                if (currentTime < dateTime || currentTime >= dateTime.AddHours(1d))
                    continue;

                color = GetColorForCount(count);
                return true;
            }

            return false;
        }

        private Color GetColorForCount(float count)
        {
            if (count < 10f)
                return Color.red;
            if (count < 20f)
                return Color.orange;
            if (count < 30f)
                return Color.yellow;
            return Color.green;
        }
    }
}
