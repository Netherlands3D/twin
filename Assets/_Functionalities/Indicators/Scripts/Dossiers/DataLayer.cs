using System;
using System.Collections.Generic;
using Netherlands3D.Functionalities.Indicators.Dossiers.Indicators;

namespace Netherlands3D.Functionalities.Indicators.Dossiers.DataLayers
{
    [Serializable]
    public struct DataLayer
    {
        public string name;
        public string unit;
        public Citation citation;
        public List<LegendItem> legend;
        public List<Frame> frames;
    }
}