using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public class ServiceMetadata
    {
        [JsonProperty("info", Required = Required.Always)]
        public Info Info { get; set; }

        [JsonProperty("externalDocs", NullValueHandling = NullValueHandling.Ignore)]
        public ExternalDocumentation? ExternalDocs { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}