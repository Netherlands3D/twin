using GeoJSON.Net.Feature;
using Netherlands3D.OgcApi;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.OgcApi.JsonConverters
{
    public static class JsonConverterInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterNewtonsoftConverters()
        {
            var previousFactory = JsonConvert.DefaultSettings;

            JsonConvert.DefaultSettings = () =>
            {
                var settings = previousFactory != null ? previousFactory() : new JsonSerializerSettings();

                // Add pagination to the FeatureCollection
                settings.Converters.Add(new ResultsConverter<FeatureCollection>());

                return settings;
            };
        }
    }
}