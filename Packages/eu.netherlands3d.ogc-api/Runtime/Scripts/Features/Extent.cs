using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi.Features
{
    [JsonObject]
    public class Extent
    {
        [JsonProperty("spatial", NullValueHandling = NullValueHandling.Ignore)]
        public SpatialExtent? Spatial { get; set; }

        [JsonProperty("temporal", NullValueHandling = NullValueHandling.Ignore)]
        public TemporalExtent? Temporal { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}