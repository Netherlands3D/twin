using System;
using GeoJSON.Net.Geometry;
using Netherlands3D.Functionalities.Indicators.Dossiers.Indicators;

namespace Netherlands3D.Functionalities.Indicators.Dossiers
{
    [Serializable]
    public struct ProjectArea
    {
        public string id;
        public string name;
        public MultiPolygon geometry;
        public Scores indicators;
    }
}