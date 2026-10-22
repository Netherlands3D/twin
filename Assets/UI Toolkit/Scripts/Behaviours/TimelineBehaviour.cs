using System.Collections.Generic;
using Netherlands3D.Timeline;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    
    public class TimelineBehaviour : MonoBehaviour
    {

        private Layers layersService;
        private UI.Components.Timeline timelineElement;

        private readonly List<(CityJSONLayerGameObject Layer, TimelineCityJson TimelineLayer)> timelineLayerPairs = new();

        private VisualElement root;
        
        void Awake()
        {
            layersService = App.Layers;
            timelineElement = App.UIRoot.Root.Q<UI.Components.Timeline>();
            foreach (var layer in layersService.Visualizations)
            {
                SubscribeToTimelineLayer(layer);
            }
            layersService.VisualizationCreated.AddListener(HandleLayerVisualizationAdded);
            layersService.LayerRemoved.AddListener(HandleLayerRemoved);

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

        private void HandleTimelineLayerInitialized()
        {
            UpdateTimelineElementVisibility();
        }
        
        private void UpdateTimelineElementVisibility()
        {
            var hasTimeline = false;
            foreach (var timelineLayerPair in timelineLayerPairs)
            {
                if (timelineLayerPair.TimelineLayer.TimestampCollections.Count > 0)
                {
                    hasTimeline = true;
                    continue;
                }
            }

            timelineElement.EnableInClassList("hidden", !hasTimeline);
        }
    }
    
}
