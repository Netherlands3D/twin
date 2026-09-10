using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Services;

namespace Netherlands3D
{
    public static class DebugStatExtensions
    {
        public readonly struct TimeValueSummary
        {
            public int Count { get; }
            public double Average { get; }
            public double Minimum { get; }
            public double Maximum { get; }

            public bool HasValues => Count > 0;
            
            public TimeValueSummary(int count, double average, double minimum, double maximum)
            {
                Count = count;
                Average = average;
                Minimum = minimum;
                Maximum = maximum;
            }
        }
        
        public static TimeValueSummary Summarize(this List<DebugStat.TimedValue> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (values.Count == 0)
                return default;

            var firstValue = values[0].Value;

            var total = firstValue;
            var minimum = firstValue;
            var maximum = firstValue;

            for (var i = 1; i < values.Count; i++)
            {
                var value = values[i].Value;
                total += value;
                if (value < minimum) minimum = value;
                if (value > maximum) maximum = value;
            }

            return new TimeValueSummary(values.Count, (float)(total / values.Count), minimum, maximum);
        }
    }
}
