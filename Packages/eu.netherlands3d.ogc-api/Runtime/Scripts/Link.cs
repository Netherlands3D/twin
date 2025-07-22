using System.Collections.Generic;
using System.Linq;
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

        public bool IsOfFormat(string[] format)
        {
            return format.Contains(Type);
        }

        public bool IsOfFormat(string format)
        {
            return string.Compare(format, Type, System.StringComparison.OrdinalIgnoreCase) == 0;
        }

        public bool IsTypeOfRelation(string[] type)
        {
            return type.Contains(Rel);
        }

        public bool IsTypeOfRelation(string type)
        {
            return string.Compare(type, Rel, System.StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}