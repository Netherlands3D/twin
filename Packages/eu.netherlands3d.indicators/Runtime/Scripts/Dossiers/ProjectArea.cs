using System;
using Netherlands3D.Indicators.Dossiers.Indicators;

namespace Netherlands3D.Indicators.Dossiers
{
    [Serializable]
    public struct ProjectArea
    {
        public string id;
        public string name;
        public Scores indicators;
    }
}