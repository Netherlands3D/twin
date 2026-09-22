using System;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.CityJson.Visualisation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Sun;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Timeline
{
    [RequireComponent(typeof(CityJSONLayerGameObject))]
    public class TimelineCityJson : MonoBehaviour
    {
        CityJSONLayerGameObject visualization;
        private Dictionary<CityObject, TimestampCollection> timelines = new();

        void Start()
        {
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
                }
            }
            //todo: use the imported data
        }
    }
}