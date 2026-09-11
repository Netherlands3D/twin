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
        GeoJSONLineLayer lines;
        private TimelineLayerPropertyData timelineLayerPropertyData;
        ColorPropertyData stylingPropertyData;

        void Start()
        {
            lines = GetComponent<GeoJSONLineLayer>();
            lines.InitProperty<TimelineLayerPropertyData>(lines.LayerData.LayerProperties);
            timelineLayerPropertyData = lines.LayerData.GetProperty<TimelineLayerPropertyData>();

            stylingPropertyData = lines.LayerData.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();

            if (stylingPropertyData == null) return;

            ServiceLocator.GetService<SunTime>().timeOfDayChanged.AddListener(OnTimeChanged);
        }

        private void OnTimeChanged(DateTime currentTime)
        {
            // var currentState = GetBuildState(currentTime);
            // SetVisibility(currentState == BuildState.Normal);
            
            var stylingPropertyData = lines.LayerData.ParentLayer.LayerProperties.GetDefaultStylingPropertyData<ColorPropertyData>();
            var color = GetColorForFeature(currentTime);
            Debug.Log(color);
            stylingPropertyData.ColorType = Symbolizer.StrokeColorProperty;
            stylingPropertyData.SetDefaultSymbolizerColor(color);
            
            lines.LineRenderer3D.SetAllColors(color);
        }

        private void SetVisibility(bool visible)
        {
            lines.LineRenderer3D.enabled = visible;
        }

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
            foreach (var feature in lines.SpawnedVisualisations.Keys)
            {
                Debug.Log(feature.Properties["hour"]);
                if (DateTime.TryParse(feature.Properties["timestamp"].ToString(), out var dateTime))
                {
                    if(float.TryParse(feature.Properties["count"].ToString(), out var count))
                    {
                        Debug.Log("current feature time: " + dateTime.ToString("yy-MM-dd HH:mm") + "\tcount: " + count);
                        if (currentTime > dateTime)
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
            if(count < 2.5f)
                return Color.red;
            if (count < 5f)
                return Color.orange;
            if(count < 7.5f)
                return Color.yellow;
            return Color.green;
        }
    }
}