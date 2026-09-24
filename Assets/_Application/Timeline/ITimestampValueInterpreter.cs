using UnityEngine;

namespace Netherlands3D.Timeline
{
    public interface ITimestampValueInterpreter
    {
        public void ProcessNewCollection(TimestampCollection newTimestampCollection);
        Color? GetColorForTimestamp(Timestamp timestamp);
        // bool Supports(TimestampCollection collection);
    }
}
