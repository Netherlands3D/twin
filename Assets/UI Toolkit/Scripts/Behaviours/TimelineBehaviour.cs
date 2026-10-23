using System;
using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.Timeline;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    public class TimelineBehaviour : MonoBehaviour
    {
        private UI.Components.Timeline timelineElement;
        private readonly List<TimelineStatusStylingLayerPropertyData> timelinePropertyDatas = new();

        private void OnEnable()
        {
            timelineElement = App.UIRoot.Root.Q<UI.Components.Timeline>();
            var currentTime = ProjectData.Current.CurrentDateTime;
            timelineElement.SetUnpaddedRange(currentTime, currentTime, currentTime);

            OnProjectDataChanged(ProjectData.Current);

            var layersService = App.Layers;
            layersService.LayerAdded.AddListener(HandleLayerAdded);
            layersService.LayerRemoved.AddListener(HandleLayerRemoved);
            ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);
        }

        private void OnDisable()
        {
            foreach (var layer in ProjectData.Current.RootLayer.GetFlatHierarchy())
            {
                HandleLayerRemoved(layer, false);
            }

            var layersService = App.Layers;
            layersService.LayerAdded.RemoveListener(HandleLayerAdded);
            layersService.LayerRemoved.RemoveListener(HandleLayerRemoved);
            ProjectData.Current.OnDataChanged.RemoveListener(OnProjectDataChanged);

            SetTimelineElementVisibility(false);
        }

        private void OnProjectDataChanged(ProjectData newProjectData)
        {
            foreach (var layer in newProjectData.RootLayer.GetFlatHierarchy())
            {
                HandleLayerAdded(layer);
            }
        }

        private void HandleLayerAdded(LayerData layer)
        {
            var timelineProperty = layer.GetProperty<TimelineStatusStylingLayerPropertyData>();
            if (timelineProperty != null)
            {
                SubscribeToTimelineProperty(timelineProperty);
            }

            layer.PropertySet.AddListener(OnLayerPropertyAdded);
            layer.PropertyRemoved.AddListener(OnLayerPropertyRemoved);
        }


        private void OnLayerPropertyAdded(LayerPropertyData newProperty)
        {
            if (newProperty is TimelineStatusStylingLayerPropertyData timelineProperty)
            {
                SubscribeToTimelineProperty(timelineProperty);
            }
        }
        
        private void OnLayerPropertyRemoved(LayerPropertyData removedProperty)
        {
            if (removedProperty is TimelineStatusStylingLayerPropertyData timelineProperty)
            {
                UnsubscribeToTimelineProperty(timelineProperty);
            }
        }

        private void HandleLayerRemoved(LayerData removedLayer, bool isRemovalRoot)
        {
            var timelineProperty = removedLayer.GetProperty<TimelineStatusStylingLayerPropertyData>();
            if (timelineProperty != null)
            {
                UnsubscribeToTimelineProperty(timelineProperty);
            }

            removedLayer.PropertySet.RemoveListener(OnLayerPropertyAdded);
        }

        private void SubscribeToTimelineProperty(TimelineStatusStylingLayerPropertyData timelineProperty)
        {
            timelinePropertyDatas.Add(timelineProperty);
            timelineProperty.TimestampCollectionAddded.AddListener(HandleTimestampCollectionAdded);
            UpdateTimelineElementVisibility();
        }

        private void UnsubscribeToTimelineProperty(TimelineStatusStylingLayerPropertyData timelineProperty)
        {
            timelinePropertyDatas.Remove(timelineProperty);
            timelineProperty.TimestampCollectionAddded.RemoveListener(HandleTimestampCollectionAdded);
            UpdateTimelineElementVisibility();
        }

        private void HandleTimestampCollectionAdded(CityObject co, TimestampCollection timestampCollection)
        {
            var addedMin = timestampCollection.LowestTimestamp;
            var addedMax = timestampCollection.HighestTimestamp;
            timelineElement.SetRange(addedMin.timestamp, addedMax.timestamp);
        }

        private void UpdateTimelineElementVisibility()
        {
            var hasTimeline = timelinePropertyDatas.Count > 0;
            SetTimelineElementVisibility(hasTimeline);
        }

        private void SetTimelineElementVisibility(bool visible)
        {
            timelineElement.UpdateTimelineVisibility(visible);
        }
    }
}