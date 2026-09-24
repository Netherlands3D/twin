using UnityEngine;

namespace Netherlands3D.Timeline
{
    public interface ITimestampValueInterpreter
    {
        Color? CalculateColor(Timestamp timestamp);
        // bool Supports(TimestampCollection collection);
    }
}
