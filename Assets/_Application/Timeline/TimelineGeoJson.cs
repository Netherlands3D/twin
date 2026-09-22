using System;
using System.Collections.Generic;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Timeline
{
    [RequireComponent(typeof(GeoJsonLayerGameObject))]
    public class TimelineGeoJson : MonoBehaviour
    {
        private SunTime sunTime;
        private GeoJsonLayerGameObject visualization;

        private TimelineStylingLayerPropertyData timelineStylingLayerPropertyData;
        private Dictionary<Feature, TimestampCollection> timelines = new();

        private float minValue = Mathf.Infinity;
        private float maxValue = Mathf.NegativeInfinity;
        
        [SerializeField] private Color minColor = Color.red;
        [SerializeField] private Color maxColor = Color.green;

        private void Start()
        {
            visualization = GetComponent<GeoJsonLayerGameObject>();
            // visualization.InitProperty<TimelineStylingLayerPropertyData>(visualization.LayerData.LayerProperties);
            visualization.OnFeatureAdd.AddListener(OnFeatureAdded);

            timelineStylingLayerPropertyData = visualization.LayerData.GetProperty<TimelineStylingLayerPropertyData>();

            sunTime = ServiceLocator.GetService<SunTime>();
            sunTime.timeOfDayChanged.AddListener(OnTimeChanged);
        }

        private void OnFeatureAdded(Feature feature)
        {
            if (feature.Properties.TryGetValue("timestamps", out var timestampObject))
            {
                var timeline = new TimestampCollection(timestampObject.ToString());
                timelines.Add(feature, timeline);
                var currentTimestamp = timeline.GetCurrentTimestamp(sunTime.Time);
                UpdateMinMax(timeline);
                SetFeatureColor(feature, currentTimestamp);
            }
        }

        private void UpdateMinMax(TimestampCollection timeline)
        {
            minValue = timeline.MinFloatValue < minValue ? timeline.MinFloatValue : minValue;
            maxValue = timeline.MaxFloatValue > maxValue ? timeline.MaxFloatValue : maxValue;
            OnTimeChanged(sunTime.Time); //recalculate the feature colors because the min/max changed
        }

        private void OnTimeChanged(DateTime currentTime)
        {
            foreach (var timelines in timelines)
            {
                var currentTimestampForFeature = timelines.Value.GetCurrentTimestamp(currentTime);
                SetFeatureColor(timelines.Key, currentTimestampForFeature);
            }
        }

        private void SetFeatureColor(Feature feature, Timestamp timestamp)
        {
            //each feature should have a unique id.
            //set styling rules here per feature
            // in GeoJsonLayerFeatureColoring: make read the styling rules after the per material styling rules (preferably this is done at once, but idk how
            //change geojson point/line/polygon to accept more colors per featyre.

            var colorAtCurrentTime = CalculateColorForFeature(feature, timestamp.timestamp);
            var useStroke = feature.Geometry.Type == GeoJSONObjectType.LineString || feature.Geometry.Type == GeoJSONObjectType.MultiLineString;
            var colorType = useStroke ? Symbolizer.StrokeColorProperty :  Symbolizer.FillColorProperty;
            timelineStylingLayerPropertyData.SetColorForFeatureById(feature.GetHashCode().ToString(), colorType, colorAtCurrentTime);
        }

        private Color? CalculateColorForFeature(Feature feature, DateTime currentTime)
        {
            var timeline = timelines[feature];
            var currentTimeStamp = timeline.GetCurrentTimestamp(currentTime);
            if(!currentTimeStamp.ValueAsFloat.HasValue)
                return null;
            
            var t = Mathf.InverseLerp(minValue, maxValue, currentTimeStamp.ValueAsFloat.Value);
            return Color.Lerp(minColor, maxColor, t);
        }
    }
}