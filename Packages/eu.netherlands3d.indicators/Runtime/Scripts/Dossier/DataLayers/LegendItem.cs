using System;
using Netherlands3D.Indicators.JsonConverters;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Indicators.Dossier.DataLayers
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