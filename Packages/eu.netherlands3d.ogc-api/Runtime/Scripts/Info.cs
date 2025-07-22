using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public class Info
    {
        [JsonProperty("title", Required = Required.Always)]
        public string Title { get; set; }

        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("termsOfService", NullValueHandling = NullValueHandling.Ignore)]
        public string? TermsOfService { get; set; }

        [JsonProperty("contact", NullValueHandling = NullValueHandling.Ignore)]
        public Contact? Contact { get; set; }

        [JsonProperty("license", NullValueHandling = NullValueHandling.Ignore)]
        public License? License { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}