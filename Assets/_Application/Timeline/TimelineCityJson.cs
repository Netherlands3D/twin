using System;
using System.Collections.Generic;
using GeoJSON.Net;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.LayerStyles;
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
        private SunTime sunTime;
        private CityJSONLayerGameObject visualization;
        
        private TimelineStylingLayerPropertyData timelineStylingLayerPropertyData;
        private Dictionary<CityObject, TimestampCollection> timelines = new();
        
        private ITimestampValueInterpreter valueInterpreter = new TimestampValueStatusInterpreter(); //todo: make this changable

        private void Start()
        {
            sunTime = ServiceLocator.GetService<SunTime>();
            sunTime.timeOfDayChanged.AddListener(OnTimeChanged);
            
            visualization = GetComponent<CityJSONLayerGameObject>();
            visualization.InitProperty<TimelineStylingLayerPropertyData>(visualization.LayerData.LayerProperties);
            visualization.CityJson.onAllCityObjectsProcessed.AddListener(ReadTimeLineFromAttributes);
        }

        private void ReadTimeLineFromAttributes()
        {
            foreach (var co in visualization.CityJson.CityObjects)
            {
                if (co.Attributes.TryGetValue("timestamps", out var attribute))
                {
                    var timeline = new TimestampCollection(attribute.Value.ToString());
                    timelines.Add(co, timeline);
                    var currentTimestamp = timeline.GetCurrentTimestamp(sunTime.Time);
                    SetFeatureColor(co, currentTimestamp);
                }
            }
        }
        
        private void OnTimeChanged(DateTime currentTime)
        {
            foreach (var timelines in timelines)
            {
                var currentTimestampForFeature = timelines.Value.GetCurrentTimestamp(currentTime);
                SetFeatureColor(timelines.Key, currentTimestampForFeature);
            }
        }

        private void SetFeatureColor(CityObject cityObject, Timestamp currentTimeStamp)
        {
            //each feature should have a unique id.
            //set styling rules here per feature
            // in GeoJsonLayerFeatureColoring: make read the styling rules after the per material styling rules (preferably this is done at once, but idk how
            //change geojson point/line/polygon to accept more colors per featyre.

            var colorAtCurrentTime = valueInterpreter.CalculateColor(currentTimeStamp);
            var colorType = Symbolizer.FillColorProperty;
            timelineStylingLayerPropertyData.SetColorForFeatureById(cityObject.GetHashCode().ToString(), colorType, colorAtCurrentTime);
        }
    }
}