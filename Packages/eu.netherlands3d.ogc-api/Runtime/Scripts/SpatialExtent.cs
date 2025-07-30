using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public record SpatialExtent
    {
        [JsonProperty("bbox", Required = Required.Always)]
        public double[][] Bbox { get; set; }

        [JsonProperty("crs", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public Uri Crs { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}