using System;
using Netherlands3D.Json.JsonConverters;
using Newtonsoft.Json;

namespace Netherlands3D.Indicators.Dossiers.DataLayers
{
    [Serializable]
    public struct Frame
    {
        public string label;

        [JsonConverter(typeof(UriConverter))]
        public Uri data;
        
        [JsonConverter(typeof(UriConverter))]
        public Uri map;

        public EsriRasterData parsedEsriRasterData;     
    }
}