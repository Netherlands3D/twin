using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public class TemporalExtent
    {
        [JsonProperty("interval", Required = Required.Always, NullValueHandling = NullValueHandling.Include)]
        public string[][] Interval { get; set; }

        [JsonProperty("trs", NullValueHandling = NullValueHandling.Ignore)]
        public string Trs { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}