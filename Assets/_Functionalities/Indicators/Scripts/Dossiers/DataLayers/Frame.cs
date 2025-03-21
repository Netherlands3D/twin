﻿using System;
using Netherlands3D.Functionalities.Indicators.Esri;
using Netherlands3D.Json.JsonConverters;
using Newtonsoft.Json;

namespace Netherlands3D.Functionalities.Indicators.Dossiers.DataLayers
{
    [Serializable]
    public class Frame
    {
        public string label;

        [JsonConverter(typeof(UriConverter))]
        public Uri data;
        
        [JsonConverter(typeof(UriConverter))]
        public Uri map;

        [NonSerialized] public EsriRasterData mapData; 
    }
}