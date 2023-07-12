using System;
using Netherlands3D.Indicators.Dossier.Indicators;

namespace Netherlands3D.Indicators.Dossier
{
    [Serializable]
    public struct ProjectArea
    {
        public string id;
        public string name;
        public Scores indicators;
    }
}