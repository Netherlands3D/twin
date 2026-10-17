using System;
using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Services;
using Netherlands3D.Sun;
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

        private void OnEnable()
        {
            sunTime = ServiceLocator.GetService<SunTime>();
            sunTime.timeOfDayChanged.AddListener(OnTimeChanged);

            visualization = GetComponent<CityJSONLayerGameObject>();
            cityJson = GetComponent<CityJSON>();
            cityJson.onAllCityObjectsProcessed.AddListener(ReadTimeLineFromAttributes);
        }

        private void OnDisable()
        {
            sunTime.timeOfDayChanged.RemoveListener(OnTimeChanged);
            timelineStatusStylingLayerPropertyData?.OnStylingChanged.RemoveListener(visualization.ApplyStyling);
            cityJson.onAllCityObjectsProcessed.RemoveListener(ReadTimeLineFromAttributes);
        }
        
        private void InitTimelineStylingProperty()
        {
            if(timelineStatusStylingLayerPropertyData != null)
                return; //already initialized
            
            visualization.InitProperty<TimelineStatusStylingLayerPropertyData>(visualization.LayerData.LayerProperties);

            timelineStatusStylingLayerPropertyData = visualization.LayerData.GetProperty<TimelineStatusStylingLayerPropertyData>();
            timelineStatusStylingLayerPropertyData.OnStylingChanged.AddListener(visualization.ApplyStyling);
        }

        private void OnDestroy()
        {

        }

        private void ReadTimeLineFromAttributes()
        {
            Dictionary<string, Color> newStates = new Dictionary<string, Color>();

            foreach (var co in cityJson.CityObjects)
            {
                if (co.Attributes.TryGetValue(TIMELINE_ATTRIBUTE_NAME, out var attribute))
                {
                    InitTimelineStylingProperty();
                    visualization.AddStylingFeatureForCityObject(co);
                    // stylingFeature.Attributes.Add(TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier, null);

                    var collection = new TimestampCollection(attribute.Value.ToString());
                    timelines.Add(co, collection);
                    
                    foreach (var timestamp in collection.Timestamps)
                    {
                        newStates.TryAdd(timestamp.value, visualization.LayerData.Color);
                    }
                    
                    SetFeatureAttribute(sunTime.Time, co, collection);
                }
            }
            if(timelineStatusStylingLayerPropertyData == null)
            {
                Destroy(this); //this CityJSON does not have timeline attributes, InitTimelineStylingProperty is never called, and we do not need this component anymore
                return;
            }
            
            // todo: if rules.Count > maxValue -> melding naar gebruiker dat het misschien niet goed gaat (maxValue is tbd)
            timelineStatusStylingLayerPropertyData.AddNewRulesForStatuses(newStates);
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
                
                if(layerFeature.Attributes.TryGetValue(TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier, out var savedStatus))
                    if(savedStatus == status)
                        continue;
                
                layerFeature.Attributes[TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier] = status;
                visualization.ApplyStylingToFeature(layerFeature);
            }
        }
    }
}