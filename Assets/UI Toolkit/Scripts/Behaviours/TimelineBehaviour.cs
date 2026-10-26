using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Timeline;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    
    public class TimelineBehaviour : MonoBehaviour
    {
        
        private readonly List<(CityJSONLayerGameObject Layer, TimelineCityJson TimelineLayer)> timelineLayerPairs = new();
        
        private UI.Components.Timeline timelineElement;

        void Awake()
        {
            timelineElement ??= App.UIRoot.Root.Q<UI.Components.Timeline>();
        }

        void OnEnable()
        {
            var layersService = App.Layers;
            foreach (var layer in layersService.Visualizations)
            {
                SubscribeToTimelineLayer(layer);
            }
            layersService.VisualizationCreated.AddListener(HandleLayerVisualizationAdded);
            layersService.LayerRemoved.AddListener(HandleLayerRemoved);
            UpdateTimelineElementVisibility();
        }
        
        void OnDisable() 
        {
            var layersService = App.Layers;
            foreach (var layer in layersService.Visualizations)
            {
                UnsubscribeToTimelineLayer(layer);
            }
            layersService.VisualizationCreated.RemoveListener(HandleLayerVisualizationAdded);
            layersService.LayerRemoved.RemoveListener(HandleLayerRemoved);
            SetTimelineElementVisibility(false);
        }

        private void HandleLayerVisualizationAdded(LayerGameObject layerGameObject)
        {
            SubscribeToTimelineLayer(layerGameObject);
        }

        private void HandleLayerRemoved(LayerData removedLayer, bool isRemovalRoot)
        {
            for (var i = timelineLayerPairs.Count - 1; i >= 0; i--)
            {
                var timelineLayerPair = timelineLayerPairs[i];
                if (timelineLayerPair.Layer != null && timelineLayerPair.Layer.LayerData != removedLayer)
                {
                    continue;
                }

                if (timelineLayerPair.TimelineLayer != null)
                {
                    timelineLayerPair.TimelineLayer.TimelineInitialized.RemoveListener(HandleTimelineLayerInitialized);
                }
                
                timelineLayerPairs.RemoveAt(i);
            }
            UpdateTimelineElementVisibility();
        }

        private void SubscribeToTimelineLayer(LayerGameObject layer)
        {
            if (layer is not CityJSONLayerGameObject cityJsonLayer) return;
            if (!cityJsonLayer.TryGetComponent<TimelineCityJson>(out var cityJsonTimelineLayer)) return;
            foreach (var timelineLayerPair in timelineLayerPairs)
            {
                if (timelineLayerPair.Layer == cityJsonLayer) return;
            }
            timelineLayerPairs.Add((cityJsonLayer, cityJsonTimelineLayer));
            cityJsonTimelineLayer.TimelineInitialized.AddListener(HandleTimelineLayerInitialized);
            UpdateTimelineElementVisibility();
        }
        
        private void UnsubscribeToTimelineLayer(LayerGameObject layer)
        {
            if (layer is not CityJSONLayerGameObject cityJsonLayer) return;
            if (!cityJsonLayer.TryGetComponent<TimelineCityJson>(out var cityJsonTimelineLayer)) return;
            cityJsonTimelineLayer.TimelineInitialized.RemoveListener(HandleTimelineLayerInitialized);
            var pairsToRemove = timelineLayerPairs.FindAll(e => e.Layer == cityJsonLayer);
            foreach (var pairToRemove in pairsToRemove)
            {
                timelineLayerPairs.Remove(pairToRemove);
            }

            UpdateTimelineElementVisibility();
        }

        private void HandleTimelineLayerInitialized()
        {
            UpdateTimelineElementVisibility();
        }
        
        private void UpdateTimelineElementVisibility()
        {
            var hasTimeline = false;

            var earliestTicks = DateTime.MaxValue.Ticks;
            var latestTicks = DateTime.MinValue.Ticks;
            
            foreach (var timelineLayerPair in timelineLayerPairs)
            {
                var timelineLayer = timelineLayerPair.TimelineLayer;
                if (timelineLayer.Timelines.Count > 0)
                {
                    hasTimeline = true;

                    foreach (var timelineKvp in timelineLayer.Timelines)
                    {
                        foreach (var timestamp in timelineKvp.Value.Timestamps)
                        {
                            var ticks = timestamp.timestamp.Date.Ticks;
                            earliestTicks = Math.Min(ticks, earliestTicks);
                            latestTicks = Math.Max(ticks, latestTicks);
                            
                            UnityEngine.Debug.Log("TIME="+new DateTime(ticks).ToString());
                            
                        }
                    }
                    
                }
            }

            timelineElement.StartValue = new DateTime(earliestTicks);
            timelineElement.EndValue = new DateTime(latestTicks);

            SetTimelineElementVisibility(!hasTimeline);
        }

        private void SetTimelineElementVisibility(bool visible)
        {
            timelineElement.EnableInClassList("hidden", visible);
        }
    }
    
}
