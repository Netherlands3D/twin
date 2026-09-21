using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Services.Netherlands3D;
using UnityEngine;

namespace Netherlands3D.Twin.Services
{

    public sealed class DebugStat
    {
        
        public readonly struct TimedValue
        {
            public double Value { get; }
            public double Timestamp { get; }

            public TimedValue(double value, double timestamp)
            {
                Value = value;
                Timestamp = timestamp;
            }
        }

        public event Action<DebugStat> Updated;

        private const int HistoryCapacity = 1024;
        private readonly Queue<TimedValue> history = new Queue<TimedValue>(HistoryCapacity);

        public string DisplayName { get; private set; }

        public DebugStatCategory Category { get; private set; }
        public bool IsEnabled { get; private set; }
        public bool HasValues => history.Count > 0;

        public DebugStat(string displayName, DebugStatCategory category, bool isEnabled = true)
        {
            DisplayName = displayName;
            Category = category;
            IsEnabled = isEnabled;
        }

        public void AddValue(double value)
        {
            var timedValue = new TimedValue(value, Time.realtimeSinceStartupAsDouble);
            if (history.Count >= HistoryCapacity)
                history.Dequeue();
            history.Enqueue(timedValue);

            Updated?.Invoke(this);
        }

        
        public void GetValuesInRangeRelative(double startTimeOffset, double endTimeOffset, List<TimedValue> result)
        {
            var currentTime = Time.realtimeSinceStartupAsDouble;
            GetValuesInRange(currentTime + startTimeOffset, currentTime + endTimeOffset, result);
        }

        public void GetValuesInRange(double startTime, double endTime, List<TimedValue> result)
        {
            result.Clear();

            foreach (var timedValue in history)
            {
                if (timedValue.Timestamp < startTime)
                    continue;

                if (timedValue.Timestamp >= endTime)
                    continue;
                
                result.Add(timedValue);
            }
        }

        public void GetValues(List<TimedValue> result)
        {
            result.Clear();
            
            foreach (var timedValue in history)
            {
                result.Add(timedValue);
            }
        }
        
    }

}