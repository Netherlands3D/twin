using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    /// <seealso href="https://docs.ogc.org/is/19-072/19-072.html#_65d42b57-7d32-4f4e-963b-ba0d4f190f27">
    /// OGC API - Common - Part 1: Core
    /// </seealso>
    [JsonObject]
    public class LandingPage
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Id { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Title { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Description { get; set; }

        [JsonProperty("attribution", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Attribution { get; set; }

        [JsonProperty("links", Required = Required.Always)]
        public Link[] Links { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}