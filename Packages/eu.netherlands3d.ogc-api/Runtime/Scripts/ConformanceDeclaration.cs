using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Netherlands3D.OgcApi
{
    [JsonObject]
    public record ConformanceDeclaration
    {
        [JsonProperty("conformsTo", Required = Required.Always)]
        public string[] ConformsTo { get; set; }

        [Preserve]
        public ConformanceDeclaration()
        {
            
        }
        
        public bool Supports(string conformanceClass)
        {
            return ConformsTo.Contains(conformanceClass);
        }
    }
}