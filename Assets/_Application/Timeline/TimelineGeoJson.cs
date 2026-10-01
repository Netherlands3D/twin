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
        const string TIMELINE_ATTRIBUTE_NAME = "timestamps";

        private SunTime sunTime;
        private GeoJsonLayerGameObject visualization;

        private TimelineStatusStylingLayerPropertyData timelineStatusStylingLayerPropertyData;
        private Dictionary<Feature, TimestampCollection> timelines = new();

        // private ITimestampValueInterpreter interpreter => timelineStylingLayerPropertyData.Interpreter; //todo: make this changable

        private void Start()
        {
            sunTime = ServiceLocator.GetService<SunTime>();
            sunTime.timeOfDayChanged.AddListener(OnTimeChanged); //todo: unsubscribe listener

            visualization = GetComponent<GeoJsonLayerGameObject>();
            visualization.InitProperty<TimelineStatusStylingLayerPropertyData>(visualization.LayerData.LayerProperties);
            visualization.OnFeatureAdd.AddListener(OnFeatureAdded); //todo: unsubscribe listener

            timelineStatusStylingLayerPropertyData = visualization.LayerData.GetProperty<TimelineStatusStylingLayerPropertyData>();
            // timelineStylingLayerPropertyData.Interpreter = new TimestampValueStatusInterpreter(visualization.LayerData.Color); //todo: use default color from styling
        }

        private void OnFeatureAdded(Feature feature)
        {
            Dictionary<string, Color> newStates = new Dictionary<string, Color>();
            if (feature.Properties.TryGetValue(TIMELINE_ATTRIBUTE_NAME, out var timestampObject))
            {
                var collection = new TimestampCollection(timestampObject.ToString());
                timelines.Add(feature, collection);

                foreach (var timestamp in collection.Timestamps)
                {
                    newStates.TryAdd(timestamp.value, visualization.LayerData.Color);
                }
                
                //todo: 
                // SetFeatureAttribute(sunTime.Time, feature, collection);
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

            var colorAtCurrentTime = timelineStatusStylingLayerPropertyData.GetColorForTimestamp(currentTimeStamp);
            var useStroke = feature.Geometry.Type == GeoJSONObjectType.LineString || feature.Geometry.Type == GeoJSONObjectType.MultiLineString;
            var colorType = useStroke ? Symbolizer.StrokeColorProperty : Symbolizer.FillColorProperty;
            // timelineStatusStylingLayerPropertyData.SetColorForFeatureById(feature.GetHashCode().ToString(), colorType, colorAtCurrentTime);
            throw new NotImplementedException(); // todo
        }
    }
}