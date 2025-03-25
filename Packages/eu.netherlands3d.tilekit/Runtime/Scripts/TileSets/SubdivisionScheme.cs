using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets", Name = "SubdivisionScheme")]
    public enum SubdivisionScheme
    {
        None,
        UniformGrid,
        Quadtree,
        Octree
    }
}