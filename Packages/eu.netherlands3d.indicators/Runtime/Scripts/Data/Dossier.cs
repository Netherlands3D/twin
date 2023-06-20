using System;
using System.Collections.Generic;

namespace Netherlands3D.Indicators.Data
{
    [Serializable]
    public struct Dossier
    {
        public string id;
        public string name;
        public Crs crs;
        public List<double> bbox;
        public List<IndicatorDefinition> indicators;
        public List<Variant> variants;
    }
}
