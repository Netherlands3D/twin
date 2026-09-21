using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleJSON;

namespace Netherlands3D.Timeline
{
    public class TimestampCollection
    {
        public List<Timestamp> timestamps = new List<Timestamp>();

        public TimestampCollection(string timestampsJsonArray)
        {
            var settings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            };

            timestamps = JsonConvert.DeserializeObject<List<Timestamp>>(timestampsJsonArray, settings);
            
            Sort();
        }

        public void Sort()
        {
            timestamps.Sort((x, y) => x.timestamp.CompareTo(y.timestamp));
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
