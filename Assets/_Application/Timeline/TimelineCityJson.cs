using System;
using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Timeline
{
    [RequireComponent(typeof(CityJSONLayerGameObject))]
    public class TimelineCityJson : TimelineObject
    {
        protected override string timelineAttributeName => "+timestamps"; //in CityJson extensions start with a +
        private CityJSON cityJson;

        protected override void Awake()
        {
            base.Awake();
            cityJson = GetComponent<CityJSON>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            cityJson.onAllCityObjectsProcessed.AddListener(ReadTimeLineFromAttributes);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            cityJson.onAllCityObjectsProcessed.RemoveListener(ReadTimeLineFromAttributes);
        }

        private void ReadTimeLineFromAttributes()
        {
            Dictionary<string, Color> newStates = new Dictionary<string, Color>();
            var cityJsonVisualization = visualization as CityJSONLayerGameObject;

            foreach (var co in cityJson.CityObjects)
            {
                if (co.Attributes.TryGetValue(timelineAttributeName, out var attribute))
                {
                    InitTimelineStylingProperty();
                    cityJsonVisualization.AddStylingFeatureForCityObject(co);
                    // stylingFeature.Attributes.Add(TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier, null);

                    var collection = new TimestampCollection(attribute.Value.ToString());
                    AddTimelineCollection(co, collection);

                    foreach (var timestamp in collection.Timestamps)
                    {
                        newStates.TryAdd(timestamp.value, visualization.LayerData.Color);
                    }

                    SetFeatureAttributeForTime(ProjectData.Current.CurrentDateTime, co, collection);
                }
            }

            if (timelineStatusStylingLayerPropertyData == null)
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

        protected override void SetFeatureAttribute(object feature, Timestamp currentTimestampForFeature)
        {
            var cityObject = (CityObject)feature;
            foreach (var visualizer in cityObject.GetComponents<CityObjectVisualizer>())
            {
                var layerFeature = visualization.LayerFeatures[visualizer];
                var status = currentTimestampForFeature?.value;

                if (layerFeature.Attributes.TryGetValue(TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier, out var savedStatus))
                    if (savedStatus == status)
                        continue;

                layerFeature.Attributes[TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier] = status;
                visualization.ApplyStylingToFeature(layerFeature);
            }
        }
    }
}