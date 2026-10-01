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

        private TimelineStylingLayerPropertyData timelineStylingLayerPropertyData;
        private Dictionary<CityObject, TimestampCollection> timelines = new();
        
        private void Start()
        {
            sunTime = ServiceLocator.GetService<SunTime>();
            sunTime.timeOfDayChanged.AddListener(OnTimeChanged); //todo: unsubscribe listener

            visualization = GetComponent<CityJSONLayerGameObject>();
            visualization.InitProperty<TimelineStylingLayerPropertyData>(visualization.LayerData.LayerProperties);

            timelineStylingLayerPropertyData = visualization.LayerData.GetProperty<TimelineStylingLayerPropertyData>();
            timelineStylingLayerPropertyData.SetDefaultColor(visualization.LayerData.Color);
            cityJson = GetComponent<CityJSON>();
            cityJson.onAllCityObjectsProcessed.AddListener(ReadTimeLineFromAttributes); //todo: unsubscribe listener
            ReadTimeLineFromAttributes();
        }

        private void ReadTimeLineFromAttributes()
        {
            foreach (var co in cityJson.CityObjects)
            {
                if (co.Attributes.TryGetValue(TIMELINE_ATTRIBUTE_NAME, out var attribute))
                {
                    var timeline = new TimestampCollection(attribute.Value.ToString());
                    timelines.Add(co, timeline);
                    timelineStylingLayerPropertyData.AddTimestampCollection(timeline);
                    timelineStylingLayerPropertyData.ProcessNewCollection(timeline);

                    SetFeatureAttribute(sunTime.Time, co, timeline);
                }
                // todo: rules.Count > maxValue -> melding naar gebruiker dat het misschien niet goed gaat 
                // todo: maxValue testen
            }

            timelineStylingLayerPropertyData.SetRulesForStatuses(timelineStylingLayerPropertyData.Colors); //todo: this is now no longer an interface
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
                if(status == null)
                layerFeature.Attributes[TimelineStylingLayerPropertyData.TimelineAttributeIdentifier] = status;
                visualization.ApplyStylingToFeature(layerFeature);
            }
        }
    }
}