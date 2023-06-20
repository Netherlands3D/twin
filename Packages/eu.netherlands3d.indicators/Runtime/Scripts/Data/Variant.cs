using System;
using System.Collections.Generic;

namespace Netherlands3D.Indicators.Data
{
    [Serializable]
    public struct Variant
    {
        public string geometry;
        public List<Area> areas;
        public Dictionary<string, SourceData> maps;
    }
}