using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
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

        [CanBeNull]
        public Collection FindById(string id)
        {
            return Items.FirstOrDefault(collection => collection.Id == id);
        }
    }
}