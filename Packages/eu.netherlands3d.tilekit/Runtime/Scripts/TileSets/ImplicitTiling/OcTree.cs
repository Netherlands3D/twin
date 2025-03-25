using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.implicit_tiling", Name = "OcTree")]
    public class OcTree : ImplicitTilingScheme
    {
        protected override SubdivisionScheme SubdivisionScheme => SubdivisionScheme.Octree;

        public int SubtreeLevels;
        public int AvailableLevels;
    }
}