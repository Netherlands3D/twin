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
        private List<Timestamp> timestamps = new List<Timestamp>();
        public float MinFloatValue { get; private set; }
        public float MaxFloatValue { get; private set; }

        public TimestampCollection(string timestampsJsonArray)
        {
            var settings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };

            timestamps = JsonConvert.DeserializeObject<List<Timestamp>>(timestampsJsonArray, settings);

            Sort();
            CalculateMinMax();
        }

        private void Sort()
        {
            timestamps.Sort((x, y) => x.timestamp.CompareTo(y.timestamp));
        }

        private void CalculateMinMax()
        {
            var timestampsWithFloatValues = timestamps.Where(t => t.ValueAsFloat.HasValue);
            MinFloatValue = timestampsWithFloatValues.Min(t => t.ValueAsFloat.Value);
            MaxFloatValue = timestampsWithFloatValues.Max(t => t.ValueAsFloat.Value);
        }

        public Timestamp GetCurrentTimestamp(DateTime currentTime)
        {
            Timestamp current = timestamps[0];

            foreach (var t in timestamps)
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