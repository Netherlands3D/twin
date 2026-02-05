using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "Metadata")]
    public class Metadata
    {
        [DataMember(Name = "name")] public string Name { get; set; }

        [DataMember(Name = "data")] public Dictionary<string, string> Data { get; } = new();
    }
}