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

        public TimestampCollection(string timestampsJsonArray)
        {
            var settings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };

            Timestamps = JsonConvert.DeserializeObject<List<Timestamp>>(timestampsJsonArray, settings);

            Sort();
        }

        private void Sort()
        {
            Timestamps.Sort((x, y) => x.timestamp.CompareTo(y.timestamp));
        }

        public Timestamp GetCurrentTimestamp(DateTime currentTime)
        {
            Timestamp current = Timestamps[0];
            if (currentTime < current.timestamp)
                return null;

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