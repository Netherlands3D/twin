using System;
using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Timeline
{
    [RequireComponent(typeof(CityJSONLayerGameObject))]
    public class TimelineCityJson : MonoBehaviour
    {
        const string TIMELINE_ATTRIBUTE_NAME = "+timestamps"; //in CityJson extensions start with a +

        private CityJSONLayerGameObject visualization;
        private CityJSON cityJson;

        private TimelineStatusStylingLayerPropertyData timelineStatusStylingLayerPropertyData;
        
        private TimelineLegendBehaviour timelineLegendBehaviour;

        private void Awake()
        {
            timelineLegendBehaviour = ServiceLocator.GetService<TimelineLegendBehaviour>();
        }

        private void OnEnable()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.AddListener(OnTimeChanged);

            visualization = GetComponent<CityJSONLayerGameObject>();
            cityJson = GetComponent<CityJSON>();
            cityJson.onAllCityObjectsProcessed.AddListener(ReadTimeLineFromAttributes);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.RemoveListener(OnTimeChanged);
            timelineStatusStylingLayerPropertyData?.OnStylingChanged.RemoveListener(visualization.ApplyStyling);
            cityJson.onAllCityObjectsProcessed.RemoveListener(ReadTimeLineFromAttributes);
            
            visualization.LayerData.LayerSelected.RemoveListener(EnableLegend);
            visualization.LayerData.LayerDeselected.RemoveListener(DisableLegend);
        }

        private void InitTimelineStylingProperty()
        {
            if(timelineStatusStylingLayerPropertyData != null)
                return; //already initialized
            
            visualization.InitProperty<TimelineStatusStylingLayerPropertyData>(visualization.LayerData.LayerProperties);

            timelineStatusStylingLayerPropertyData = visualization.LayerData.GetProperty<TimelineStatusStylingLayerPropertyData>();
            timelineStatusStylingLayerPropertyData.OnStylingChanged.AddListener(visualization.ApplyStyling);
            
            visualization.LayerData.LayerSelected.AddListener(EnableLegend);
            visualization.LayerData.LayerDeselected.AddListener(DisableLegend);
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
                    AddTimelineCollection(co, collection);
                    
                    foreach (var timestamp in collection.Timestamps)
                    {
                        newStates.TryAdd(timestamp.value, visualization.LayerData.Color);
                    }
                    
                    SetFeatureAttribute(ProjectData.Current.CurrentDateTime, co, collection);
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

        private void AddTimelineCollection(CityObject co, TimestampCollection collection)
        {
            timelineStatusStylingLayerPropertyData.AddTimestampCollection(co, collection);
        }

        private void OnTimeChanged(DateTime currentTime)
        {
            foreach (var timeline in timelineStatusStylingLayerPropertyData.TimestampCollections)
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
        
        private void EnableLegend(LayerData layer)
        {
            timelineLegendBehaviour.ShowLegend(layer, true);
        }
        
        private void DisableLegend(LayerData layer)
        {
            timelineLegendBehaviour.ShowLegend(layer, false);
        }
    }
}