using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Timeline
{
    public class TimestampValueStatusInterpreter : ITimestampValueInterpreter
    {
        [SerializeField] private Dictionary<string, Color> colors; //todo: this is not a monobehaviour and therefore not serialized
        public Color? CalculateColor(Timestamp timestamp)
        {
            if (colors.TryGetValue(timestamp.value, out var color))
            {
                return color;
            }

            return null;
        }
    }
}
