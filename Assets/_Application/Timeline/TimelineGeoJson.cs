using System;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Timeline
{
    [RequireComponent(typeof(GeoJsonLayerGameObject))]
    public class TimelineGeoJson : TimelineObject
    {
        protected override string TIMELINE_ATTRIBUTE_NAME => "timestamps";
        GeoJsonLayerGameObject geoJson => visualization as GeoJsonLayerGameObject;

        protected override void OnEnable()
        {
            base.OnEnable();
            // ProjectData.Current.OnCurrentDateTimeChanged.AddListener(OnTimeChanged);
            geoJson.OnFeatureAdd.AddListener(OnAddFeature);
            geoJson.OnFeatureRemove.AddListener(OnRemoveFeature);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            geoJson.OnFeatureAdd.RemoveListener(OnAddFeature);
            geoJson.OnFeatureRemove.RemoveListener(OnRemoveFeature);
        }

        private void OnAddFeature(Feature feature)
        {
            Dictionary<string, Color> newStates = new Dictionary<string, Color>();
            // foreach (var feature in geoJson.Features)
            // {
                if (feature.Properties.TryGetValue(TIMELINE_ATTRIBUTE_NAME, out var attribute))
                {
                    InitTimelineStylingProperty();
                    geoJson.AddStylingFeatureForGeoJsonFeature(feature);
                    // stylingFeature.Attributes.Add(TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier, null);

                    var collection = new TimestampCollection(attribute.ToString()); //todo make overload to accept json array directly
                    timelineStatusStylingLayerPropertyData.AddTimestampCollection(feature, collection);

                    foreach (var timestamp in collection.Timestamps)
                    {
                        newStates.TryAdd(timestamp.value, visualization.LayerData.Color);
                    }

                    SetFeatureAttributeForTime(ProjectData.Current.CurrentDateTime, feature, collection);
                }
            // }

            if (timelineStatusStylingLayerPropertyData == null)
            {
                Destroy(this); //this CityJSON does not have timeline attributes, InitTimelineStylingProperty is never called, and we do not need this component anymore
                return;
            }

            // todo: if rules.Count > maxValue -> melding naar gebruiker dat het misschien niet goed gaat (maxValue is tbd)
            timelineStatusStylingLayerPropertyData.AddNewRulesForStatuses(newStates);
        }

        protected override void SetFeatureAttribute(object feature, Timestamp currentTimestampForFeature)
        {
            var geoJsonFeature = (Feature)feature;
            throw new NotImplementedException();
        }

        private void OnRemoveFeature(Feature feature)
        {
            geoJson.RemoveStylingFeatureForGeoJsonFeature(feature);
        }
    }
}