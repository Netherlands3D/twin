using System.Linq;
using Newtonsoft.Json;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public record ConformanceDeclaration
    {
        [JsonProperty("conformsTo", Required = Required.Always)]
        public string[] ConformsTo { get; set; }

        public bool Supports(string conformanceClass)
        {
            return ConformsTo.Contains(conformanceClass);
        }
    }
}