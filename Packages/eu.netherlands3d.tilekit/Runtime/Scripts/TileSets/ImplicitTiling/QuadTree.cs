using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.implicit_tiling", Name = "QuadTree")]
    public class QuadTree : ImplicitTilingScheme
    {
        protected override SubdivisionScheme SubdivisionScheme => SubdivisionScheme.Quadtree;

        public int SubtreeLevels;
        public int AvailableLevels;
    }
}