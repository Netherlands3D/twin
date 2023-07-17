using System;
using System.Collections.Generic;

namespace Netherlands3D.Indicators.Dossiers
{
    [Serializable]
    public struct Variant
    {
        public string geometry;
        public List<ProjectArea> areas;
        public Dictionary<string, DataLayer> maps;
    }
}