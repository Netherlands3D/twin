using System;
using System.Collections.Generic;
using GeoJSON.Net.Converters;
using GeoJSON.Net.CoordinateReferenceSystem;
using Netherlands3D.Indicators.Dossier;
using Netherlands3D.Indicators.Dossier.Indicators;
using Newtonsoft.Json;

namespace Netherlands3D.Indicators.Dossier
{
    [Serializable]
    public struct Dossier
    {
        public string id;
        public string name;
        
        /// <summary>
        /// The exact same CRS definition as GeoJSON has; so we re-use the GeoJSON.net Crs converter
        /// </summary>
        [JsonProperty(PropertyName = "crs", Required = Required.Default, DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Include)]
        [JsonConverter(typeof(CrsConverter))]
        public ICRSObject crs;
        
        [JsonProperty(PropertyName = "bbox", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public List<double> bbox;

        public List<Definition> indicators;
        public List<Variant> variants;
    }
}