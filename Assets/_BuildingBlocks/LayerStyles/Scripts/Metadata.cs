using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "Metadata")]
    public struct Metadata
    {
        public string Name { get; }
    }
}