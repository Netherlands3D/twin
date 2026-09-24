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
        
        private ITimestampValueInterpreter interpreter => timelineStylingLayerPropertyData.Interpreter; //todo: make this changable
        
        private void Start()
        {
            sunTime = ServiceLocator.GetService<SunTime>();
            sunTime.timeOfDayChanged.AddListener(OnTimeChanged);
            
            visualization = GetComponent<GeoJsonLayerGameObject>();
            visualization.InitProperty<TimelineStylingLayerPropertyData>(visualization.LayerData.LayerProperties);
            visualization.OnFeatureAdd.AddListener(OnFeatureAdded);

            timelineStylingLayerPropertyData = visualization.LayerData.GetProperty<TimelineStylingLayerPropertyData>();
            timelineStylingLayerPropertyData.Interpreter = new TimestampValueStatusInterpreter(visualization.LayerData.Color); //todo: use default color from styling
        }

        private void OnFeatureAdded(Feature feature)
        {
            if (feature.Properties.TryGetValue("timestamps", out var timestampObject))
            {
                var timeline = new TimestampCollection(timestampObject.ToString());
                timelines.Add(feature, timeline);
                timelineStylingLayerPropertyData.AddTimestampCollection(timeline);
                
                var currentTimestamp = timeline.GetCurrentTimestamp(sunTime.Time);
                SetFeatureColor(feature, currentTimestamp);
            }
        }
        
        private void OnTimeChanged(DateTime currentTime)
        {
            foreach (var timelines in timelines)
            {
                var currentTimestampForFeature = timelines.Value.GetCurrentTimestamp(currentTime);
                SetFeatureColor(timelines.Key, currentTimestampForFeature);
            }
        }

        private void SetFeatureColor(Feature feature, Timestamp currentTimeStamp)
        {
            //each feature should have a unique id.
            //set styling rules here per feature
            // in GeoJsonLayerFeatureColoring: make read the styling rules after the per material styling rules (preferably this is done at once, but idk how
            //change geojson point/line/polygon to accept more colors per featyre.

            var colorAtCurrentTime = interpreter.GetColorForTimestamp(currentTimeStamp);
            var useStroke = feature.Geometry.Type == GeoJSONObjectType.LineString || feature.Geometry.Type == GeoJSONObjectType.MultiLineString;
            var colorType = useStroke ? Symbolizer.StrokeColorProperty :  Symbolizer.FillColorProperty;
            timelineStylingLayerPropertyData.SetColorForFeatureById(feature.GetHashCode().ToString(), colorType, colorAtCurrentTime);
        }
    }
}