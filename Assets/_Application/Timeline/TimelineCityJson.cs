using System;
using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Timeline
{
    [RequireComponent(typeof(CityJSONLayerGameObject))]
    public class TimelineCityJson : MonoBehaviour
    {
        const string TIMELINE_ATTRIBUTE_NAME = "+timestamps"; //in CityJson extensions start with a +

        private SunTime sunTime;
        private CityJSONLayerGameObject visualization;
        private CityJSON cityJson;

        private TimelineStatusStylingLayerPropertyData timelineStatusStylingLayerPropertyData;
        private Dictionary<CityObject, TimestampCollection> timelines = new();
        
        private void Start()
        {
            sunTime = ServiceLocator.GetService<SunTime>();
            sunTime.timeOfDayChanged.AddListener(OnTimeChanged); //todo: unsubscribe listener

            visualization = GetComponent<CityJSONLayerGameObject>();
            visualization.InitProperty<TimelineStatusStylingLayerPropertyData>(visualization.LayerData.LayerProperties, null, visualization.LayerData.Color);

            timelineStatusStylingLayerPropertyData = visualization.LayerData.GetProperty<TimelineStatusStylingLayerPropertyData>();
            timelineStatusStylingLayerPropertyData.OnStylingChanged.AddListener(visualization.ApplyStyling);
            // timelineStatusStylingLayerPropertyData.SetDefaultColor(visualization.LayerData.Color);
            cityJson = GetComponent<CityJSON>();
            cityJson.onAllCityObjectsProcessed.AddListener(ReadTimeLineFromAttributes); //todo: unsubscribe listener
            ReadTimeLineFromAttributes();
        }

        private void OnDestroy()
        {
            sunTime.timeOfDayChanged.RemoveListener(OnTimeChanged); //todo: unsubscribe listener
            timelineStatusStylingLayerPropertyData.OnStylingChanged.RemoveListener(visualization.ApplyStyling);
            cityJson.onAllCityObjectsProcessed.RemoveListener(ReadTimeLineFromAttributes); //todo: unsubscribe listener
        }

        private void ReadTimeLineFromAttributes()
        {
            Dictionary<string, Color> newStates = new Dictionary<string, Color>();

            foreach (var co in cityJson.CityObjects)
            {
                if (co.Attributes.TryGetValue(TIMELINE_ATTRIBUTE_NAME, out var attribute))
                {
                    var collection = new TimestampCollection(attribute.Value.ToString());
                    timelines.Add(co, collection);
                    
                    foreach (var timestamp in collection.Timestamps)
                    {
                        newStates.TryAdd(timestamp.value, visualization.LayerData.Color);
                    }
                    
                    SetFeatureAttribute(sunTime.Time, co, collection);
                }
                // todo: rules.Count > maxValue -> melding naar gebruiker dat het misschien niet goed gaat 
                // todo: maxValue testen
            }
            timelineStatusStylingLayerPropertyData.AddRulesForStatuses(newStates);
        }
        
        private void OnTimeChanged(DateTime currentTime)
        {
            foreach (var timeline in timelines)
            {
                SetFeatureAttribute(currentTime, timeline.Key, timeline.Value);
            }
        }

        private void SetFeatureAttribute(DateTime currentTime, CityObject cityObject, TimestampCollection timeline)
        {
            var currentTimestampForFeature = timeline.GetCurrentTimestamp(currentTime);
            foreach (var visualizer in cityObject.GetComponents<CityObjectVisualizer>())
            {
                var layerFeature = visualization.LayerFeatures[visualizer];
                var status = currentTimestampForFeature?.value;
                
                if(layerFeature.Attributes[TimelineStatusStylingLayerPropertyData.TimelineAttributeIdentifier] == status)
                    continue;
                
                layerFeature.Attributes[TimelineStatusStylingLayerPropertyData.TimelineAttributeIdentifier] = status;
                visualization.ApplyStylingToFeature(layerFeature);
            }
        }
    }
}