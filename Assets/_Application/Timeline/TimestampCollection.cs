using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.ExtensionMethods;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleJSON;

namespace Netherlands3D.Timeline
{
    public class TimestampCollection
    {
        public List<Timestamp> Timestamps = new List<Timestamp>();
        public float MinFloatValue { get; private set; }
        public float MaxFloatValue { get; private set; }

        public TimestampCollection(string timestampsJsonArray)
        {
            var settings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };

            Timestamps = JsonConvert.DeserializeObject<List<Timestamp>>(timestampsJsonArray, settings);

            Sort();
            CalculateMinMax();
        }

        private void Sort()
        {
            Timestamps.Sort((x, y) => x.timestamp.CompareTo(y.timestamp));
        }

        private void CalculateMinMax()
        {
            var timestampsWithFloatValues = Timestamps.Where(t => t.ValueAsFloat.HasValue);
            MinFloatValue = timestampsWithFloatValues.Min(t => t.ValueAsFloat.Value);
            MaxFloatValue = timestampsWithFloatValues.Max(t => t.ValueAsFloat.Value);
        }

        public Timestamp GetCurrentTimestamp(DateTime currentTime)
        {
            Timestamp current = Timestamps[0];

            foreach (var t in Timestamps)
            {
                if (t.timestamp <= currentTime)
                    current = t;
                else
                    break;
            }

            return current;
        }
    }
}