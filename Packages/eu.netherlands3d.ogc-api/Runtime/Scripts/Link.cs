using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public record Link
    {
        [JsonProperty("href", Required = Required.Always)]
        public string Href { get; set; }

        [JsonProperty("rel", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Rel { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Type { get; set; }

        [JsonProperty("hreflang", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Hreflang { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        [CanBeNull]
        public string Title { get; set; }

        [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
        public int? Length { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();

        public bool IsOfMediaType(string[] mediaType)
        {
            return mediaType.Contains(Type);
        }

        public bool IsOfMediaType(string format)
        {
            return string.Compare(format, Type, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public bool IsTypeOfRelation(string[] type)
        {
            return type.Contains(Rel);
        }

        public bool IsTypeOfRelation(string type)
        {
            return string.Compare(type, Rel, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}