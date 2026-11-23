using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Timeline
{
    [RequireComponent(typeof(GeoJsonLayerGameObject))]
    public class TimelineGeoJson : TimelineObject
    {
        protected override string timelineAttributeName => "timestamps";
        GeoJsonLayerGameObject geoJson => visualization as GeoJsonLayerGameObject;

        protected override void OnEnable()
        {
            base.OnEnable();
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
            if (feature.Properties.TryGetValue(timelineAttributeName, out var attribute))
            {
                Dictionary<string, Color> newStates = new Dictionary<string, Color>();
                InitTimelineStylingProperty();
                geoJson.AddStylingFeatureForGeoJsonFeature(feature);

                var collection = new TimestampCollection(attribute.ToString()); //todo make overload to accept json array directly
                timelineStatusStylingLayerPropertyData.AddTimestampCollection(feature, collection);

                foreach (var timestamp in collection.Timestamps)
                {
                    newStates.TryAdd(timestamp.value, visualization.LayerData.Color);
                }

                SetFeatureAttributeForTime(ProjectData.Current.CurrentDateTime, feature, collection);
                
                // todo: if rules.Count > maxValue -> melding naar gebruiker dat het misschien niet goed gaat (maxValue is tbd)
                timelineStatusStylingLayerPropertyData.AddNewRulesForStatuses(newStates);
            }
        }

        protected override void SetFeatureAttribute(object feature, Timestamp currentTimestampForFeature)
        {
            var geoJsonFeature = (Feature)feature;

            var layerFeature = visualization.StylingFeatures[geoJsonFeature];
            var status = currentTimestampForFeature?.value;

            if (layerFeature.Attributes.TryGetValue(TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier, out var savedStatus))
                if (savedStatus == status)
                    return;

            layerFeature.Attributes[TimelineStatusStylingLayerPropertyData.TimelineStatusAttributeIdentifier] = status;
            
            visualization.ApplyStylingToFeature(layerFeature);
        }

        private void OnRemoveFeature(Feature feature)
        {
            geoJson.RemoveStylingFeatureForGeoJsonFeature(feature);
            timelineStatusStylingLayerPropertyData.RemoveTimestampCollection(feature);
        }
    }
}