using Newtonsoft.Json;

namespace Netherlands3D.OgcApi
{
    public record ConformanceDeclaration
    {
        // TODO: add helpers methods to have readable checks on what an OGC API can do 
        [JsonProperty("conformsTo", Required = Required.Always)]
        public string[] ConformsTo { get; set; }
    }
}