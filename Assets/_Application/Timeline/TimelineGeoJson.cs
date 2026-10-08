using System;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
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

        private void Start()
        {
            sunTime = ServiceLocator.GetService<SunTime>();
            sunTime.timeOfDayChanged.AddListener(OnTimeChanged);

            visualization = GetComponent<GeoJsonLayerGameObject>();
            visualization.OnFeatureAdd.AddListener(OnFeatureAdded);
        }

        private void OnDestroy()
        {
            sunTime.timeOfDayChanged.RemoveListener(OnTimeChanged);
            timelineStatusStylingLayerPropertyData?.OnStylingChanged.RemoveListener(visualization.ApplyStyling);
            visualization.OnFeatureAdd.RemoveListener(OnFeatureAdded);
        }

        private void InitTimelineStylingProperty()
        {
            if (timelineStatusStylingLayerPropertyData != null)
                return; //already initialized

            visualization.InitProperty<TimelineStatusStylingLayerPropertyData>(visualization.LayerData.LayerProperties);

            timelineStatusStylingLayerPropertyData = visualization.LayerData.GetProperty<TimelineStatusStylingLayerPropertyData>();
            timelineStatusStylingLayerPropertyData.OnStylingChanged.AddListener(visualization.ApplyStyling);
        }

        private void OnFeatureAdded(Feature feature)
        {
            Dictionary<string, Color> newStates = new Dictionary<string, Color>();

            if (feature.Properties.TryGetValue(TIMELINE_ATTRIBUTE_NAME, out var timestampObject))
            {
                InitTimelineStylingProperty();

                var collection = new TimestampCollection(timestampObject.ToString());
                timelines.Add(feature, collection);

                foreach (var timestamp in collection.Timestamps)
                {
                    newStates.TryAdd(timestamp.value, visualization.LayerData.Color);
                }
                CreateStylingFeatureForGeojsonFeature(feature);
                SetFeatureAttribute(sunTime.Time, feature, collection);
            }
        }

        private void OnTimeChanged(DateTime currentTime)
        {
            foreach (var timeline in timelines)
            {
                SetFeatureAttribute(currentTime, timeline.Key, timeline.Value);
            }
        }

        private void SetFeatureAttribute(DateTime currentTime, Feature feature, TimestampCollection timeline)
        {
            var currentTimestampForFeature = timeline.GetCurrentTimestamp(currentTime);

            var layerFeature = visualization.LayerFeatures[feature];
            var status = currentTimestampForFeature?.value;

            if (layerFeature.Attributes[TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier] == status)
                return;

            layerFeature.Attributes[TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier] = status;
            visualization.ApplyStylingToFeature(feature);
        }
        
        private void CreateStylingFeatureForGeojsonFeature(Feature feature)
        {
            var layerFeature = visualization.CreateFeature(feature);
            layerFeature.Attributes.Add(TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier, null);
            visualization.LayerFeatures.Add(layerFeature.Geometry, layerFeature);
        }
    }
}