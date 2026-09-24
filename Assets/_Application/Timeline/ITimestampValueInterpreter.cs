using UnityEngine;

namespace Netherlands3D.Timeline
{
    public interface ITimestampValueInterpreter
    {
        Color? GetColorForTimestamp(Timestamp timestamp);
        // bool Supports(TimestampCollection collection);
    }
}
