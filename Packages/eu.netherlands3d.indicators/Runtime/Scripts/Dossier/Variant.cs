using System;
using System.Collections.Generic;

namespace Netherlands3D.Indicators.Dossier
{
    [Serializable]
    public struct Variant
    {
        public string geometry;
        public List<ProjectArea> areas;
        public Dictionary<string, DataLayer> maps;
    }
}