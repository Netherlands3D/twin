using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public class Link
    {
        [JsonProperty("href", Required = Required.Always)]
        public string Href { get; set; }

        [JsonProperty("rel", Required = Required.Always)]
        public string Rel { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string? Type { get; set; }

        [JsonProperty("hreflang", NullValueHandling = NullValueHandling.Ignore)]
        public string? Hreflang { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string? Title { get; set; }

        [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
        public int? Length { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}