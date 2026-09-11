using System;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Unity.Profiling.Editor;
using UnityEngine;

namespace Netherlands3D
{
    [RequireComponent(typeof(LayerGameObject))]
    public class TimelineGeoJson : MonoBehaviour
    {
        GeoJsonLayerGameObject visualization;
        private TimelineLayerPropertyData timelineLayerPropertyData;
        ColorPropertyData stylingPropertyData;

        void Start()
        {
            visualization = GetComponent<GeoJsonLayerGameObject>();
            visualization.InitProperty<TimelineLayerPropertyData>(visualization.LayerData.LayerProperties);
            timelineLayerPropertyData = visualization.LayerData.GetProperty<TimelineLayerPropertyData>();

            stylingPropertyData = visualization.LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();

            if (stylingPropertyData == null) return;

            ServiceLocator.GetService<SunTime>().timeOfDayChanged.AddListener(OnTimeChanged);
        }

        private void OnTimeChanged(DateTime currentTime)
        {
            // var currentState = GetBuildState(currentTime);
            // SetVisibility(currentState == BuildState.Normal);

            var stylingPropertyData = visualization.LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();
            var color = GetColorForFeature(currentTime);
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

        private Color GetColorForFeature(DateTime currentTime)
        {
            var color = Color.white;
            foreach (var feature in visualization.GeoJsonFeatures)
            {
                Debug.Log(feature.Properties["hour"]);
                if (DateTime.TryParse(feature.Properties["timestamp"].ToString(), out var dateTime))
                {
                    if (float.TryParse(feature.Properties["count"].ToString(), out var count))
                    {
                        Debug.Log("current feature time: " + dateTime.ToString("yy-MM-dd HH:mm") + "\tcount: " + count);
                        if (currentTime > dateTime && currentTime < dateTime.AddHours(1d))
                        {
                            return GetColorForCount(count);
                        }
                    }
                }
            }

            return color;
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