using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi.Features
{
    [JsonObject]
    public class Collection
    {
        [JsonProperty("id", Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string? Title { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("links", Required = Required.Always)]
        public Link[] Links { get; set; }

        [JsonProperty("extent", NullValueHandling = NullValueHandling.Ignore)]
        public Extent? Extent { get; set; }

        [JsonProperty("itemType", NullValueHandling = NullValueHandling.Ignore)]
        public string ItemType { get; set; }

        [JsonProperty("crs", NullValueHandling = NullValueHandling.Ignore)]
        public string[]? Crs { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}