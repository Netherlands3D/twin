using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Netherlands3D.Timeline
{
    public class Timestamp
    {
        [DataMember] public int id;
        [DataMember] public DateTime timestamp;
        [DataMember] public string value;

        [JsonIgnore]
        public int? ValueAsInt
        {
            get
            {
                if (int.TryParse(value, out var result))
                    return result;
                return null;
            }
        }

        [JsonIgnore]
        public float? ValueAsFloat
        {
            get
            {
                if (float.TryParse(value, out var result))
                    return result;
                return null;
            }
        }
    }
}