using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    public class ConformanceDeclaration
    {
        // TODO: add helpers methods to have readable checks on what an OGC API can do 
        [JsonProperty("conformsTo", Required = Required.Always)]
        public string[] ConformsTo { get; set; }
    }
}