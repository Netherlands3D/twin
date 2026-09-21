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
        
        [JsonIgnore] public int? ValueAsInt => int.Parse(value);
        [JsonIgnore] public float? ValueAsFloat => float.Parse(value);
    }
}
