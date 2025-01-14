using System;

namespace Netherlands3D.Functionalities.Indicators.Dossiers.Indicators
{
    /// <summary>
    /// Whether the indicator's value should trigger a visual warning, values range from 0 to 2, where 0 is OK, 1 is WARNING and 2 is ALERT
    /// </summary>
    [Serializable]
    public enum IndicatorAlertLevel
    {
        OK = 0,
        WARNING = 1,
        ALERT = 2
    }
}