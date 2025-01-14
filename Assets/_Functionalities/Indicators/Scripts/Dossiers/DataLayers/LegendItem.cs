using System;
using Netherlands3D.Json.JsonConverters;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Functionalities.Indicators.Dossiers.DataLayers
{
    [Serializable]
    public struct LegendItem
    {
        public string label;
        public string value;
        
        [JsonConverter(typeof(ColorConverter))]
        public Color color;
    }
}