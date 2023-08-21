using System;
using System.Collections.Generic;
using Netherlands3D.Json.JsonConverters;
using Newtonsoft.Json;

namespace Netherlands3D.Indicators.Dossiers.Indicators
{
    [Serializable]
    public struct Scores
    {
        public Dictionary<string, float> Values;

        [JsonConverter(typeof(UriConverter))]
        public Uri graph;
    }
}