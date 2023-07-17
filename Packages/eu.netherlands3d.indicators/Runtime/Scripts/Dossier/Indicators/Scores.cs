using System;
using Netherlands3D.Json.JsonConverters;
using Newtonsoft.Json;

namespace Netherlands3D.Indicators.Dossier.Indicators
{
    [Serializable]
    public struct Scores
    {
        [JsonConverter(typeof(UriConverter))]
        public Uri graph;
    }
}