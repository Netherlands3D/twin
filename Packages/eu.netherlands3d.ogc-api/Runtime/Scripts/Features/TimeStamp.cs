using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi.Features
{
    [JsonObject]
    public class TimeStamp
    {
        [JsonProperty("value", Required = Required.Always)]
        public DateTime Value { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; } = new Dictionary<string, JToken>();
    }
}