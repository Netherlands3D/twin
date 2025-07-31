using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public record Contact
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Name { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Url { get; set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Email { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}