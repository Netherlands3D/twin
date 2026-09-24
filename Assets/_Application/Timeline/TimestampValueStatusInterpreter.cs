using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Timeline
{
    public class TimestampValueStatusInterpreter : ITimestampValueInterpreter
    {
        private Color defaultColor;
        private Dictionary<string, Color> colors;
        
        public UnityEvent<string, Color> OnColorInterpretationChanged;

        public TimestampValueStatusInterpreter(Color defaultColor)
        {
            this.defaultColor = defaultColor;
        }
        
        public void ProcessNewCollection(TimestampCollection newTimestampCollection)
        {
            foreach (var timestamp in newTimestampCollection.Timestamps)
            {
                colors.TryAdd(timestamp.value, defaultColor);
            }
        }
        
        public Color? GetColorForTimestamp(Timestamp timestamp)
        {
            if (colors.TryGetValue(timestamp.value, out var color))
            {
                return color;
            }

            return null;
        }

        public IList GetStates()
        {
            return colors.Keys.ToList(); //todo: is this always the same order?
        }

        public Color GetColorForStatus(string status)
        {
            return colors[status];
        }

        public void SetColorForStatus(string status, Color color)
        {
            colors[status] = color;
            OnColorInterpretationChanged.Invoke(status, color);
        }
    }
}
