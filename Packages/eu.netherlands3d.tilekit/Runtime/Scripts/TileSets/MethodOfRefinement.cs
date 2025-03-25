using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets", Name = "MethodOfRefinement")]
    public enum MethodOfRefinement
    {
        Add,
        Replace
    }
}