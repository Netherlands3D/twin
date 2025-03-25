using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.implicit_tiling", Name = "UniformGrid")]
    public class UniformGrid : ImplicitTilingScheme
    {
        protected override SubdivisionScheme SubdivisionScheme => SubdivisionScheme.UniformGrid;

        public Dimensions TileSize;
    }
}