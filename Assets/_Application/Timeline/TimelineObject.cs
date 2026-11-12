using System;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Timeline
{
    public abstract class TimelineObject : MonoBehaviour
    {
        protected abstract string timelineAttributeName { get; }

        protected LayerGameObject visualization;
        // private CityJSONLayerGameObject visualization;
        // private CityJSON cityJson;

        protected TimelineStatusStylingLayerPropertyData timelineStatusStylingLayerPropertyData;
        protected TimelineLegendBehaviour timelineLegendBehaviour;

        protected virtual void Awake()
        {
            timelineLegendBehaviour = ServiceLocator.GetService<TimelineLegendBehaviour>();
            visualization = GetComponent<LayerGameObject>();
        }
        
        protected virtual void OnEnable()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.AddListener(OnTimeChanged);
        }
        
        protected virtual void OnDisable()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.AddListener(OnTimeChanged);
        
            visualization.LayerData.LayerSelected.RemoveListener(EnableLegend);
            visualization.LayerData.LayerDeselected.RemoveListener(DisableLegend);
            timelineStatusStylingLayerPropertyData?.OnStylingChanged.RemoveListener(visualization.ApplyStyling);
        }
        
        protected void InitTimelineStylingProperty()
        {
            if(timelineStatusStylingLayerPropertyData != null)
                return; //already initialized
            
            visualization.InitProperty<TimelineStatusStylingLayerPropertyData>(visualization.LayerData.LayerProperties);

            timelineStatusStylingLayerPropertyData = visualization.LayerData.GetProperty<TimelineStatusStylingLayerPropertyData>();
            timelineStatusStylingLayerPropertyData.OnStylingChanged.AddListener(visualization.ApplyStyling);
            
            visualization.LayerData.LayerSelected.AddListener(EnableLegend);
            visualization.LayerData.LayerDeselected.AddListener(DisableLegend);
        }

        // protected abstract void ReadTimeLineFromAttributes();
        
        private void OnTimeChanged(DateTime currentTime)
        {
            foreach (var timeline in timelineStatusStylingLayerPropertyData.TimestampCollections)
            {
                SetFeatureAttributeForTime(currentTime, timeline.Key, timeline.Value);
            }
        }

        protected void SetFeatureAttributeForTime(DateTime currentTime, object feature, TimestampCollection timeline)
        {
            var currentTimestampForFeature = timeline.GetCurrentTimestamp(currentTime);
            SetFeatureAttribute(feature, currentTimestampForFeature);
        }

        protected abstract void SetFeatureAttribute(object feature, Timestamp currentTimestampForFeature);

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
