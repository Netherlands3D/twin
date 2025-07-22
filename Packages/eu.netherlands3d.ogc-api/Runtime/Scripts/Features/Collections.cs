using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi.Features
{
    [JsonObject]
    public class Collections
    {
        [JsonProperty("links", Required = Required.Always)]
        public Link[] Links { get; set; }

        [JsonProperty("collections", Required = Required.Always)]
        public Collection[] Items { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}