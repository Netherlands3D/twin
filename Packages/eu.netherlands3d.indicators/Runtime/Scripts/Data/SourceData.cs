using System;
using System.Collections.Generic;

namespace Netherlands3D.Indicators.Data
{
    [Serializable]
    public struct SourceData
    {
        public string name;
        public string unit;
        public string citation;
        public List<LegendItem> legend;
        public List<MapLayer> frames;
    }
}