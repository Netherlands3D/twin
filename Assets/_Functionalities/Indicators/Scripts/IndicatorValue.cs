using System;

namespace Netherlands3D.Indicators.Dossiers.Indicators
{
    [Serializable]
    public struct IndicatorValue
    {
        public string id;
        public float value;
        public IndicatorAlertLevel alertLevel;
    }
}