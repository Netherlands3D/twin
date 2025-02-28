using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "Metadata")]
    public class Metadata
    {
        [DataMember(Name = "name")] public string Name { get; set; }
    }
}