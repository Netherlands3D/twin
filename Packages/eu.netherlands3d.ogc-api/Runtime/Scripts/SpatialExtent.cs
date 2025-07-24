using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public class SpatialExtent
    {
        [JsonProperty("bbox", Required = Required.Always)]
        public double[][] Bbox { get; set; }

        [JsonProperty("crs", NullValueHandling = NullValueHandling.Ignore)]
        public string? Crs { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}