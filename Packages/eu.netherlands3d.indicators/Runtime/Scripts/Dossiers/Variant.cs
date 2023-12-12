using System;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Indicators.Dossiers
{
    [Serializable]
    public struct Variant
    {
        public List<ProjectArea> areas;
        public Dictionary<string, DataLayer> maps;

        public ProjectArea FindProjectAreaById(string id)
        {
            return areas.First(area => area.id == id);
        }
    }
}