using System;
using System.Collections.Generic;
using Netherlands3D.Indicators.Dossier.DataLayers;

namespace Netherlands3D.Indicators.Dossier
{
    [Serializable]
    public struct DataLayer
    {
        public string name;
        public string unit;
        public string citation;
        public List<LegendItem> legend;
        public List<Frame> frames;
    }
}