using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public class Extent
    {
        [JsonProperty("spatial", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public SpatialExtent Spatial { get; set; }

        [JsonProperty("temporal", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public TemporalExtent Temporal { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}