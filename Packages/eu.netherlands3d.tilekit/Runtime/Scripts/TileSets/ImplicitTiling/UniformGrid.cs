using System.Runtime.Serialization;
using KindMen.Uxios;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.implicit_tiling", Name = "UniformGrid")]
    public struct UniformGrid : IImplicitTilingScheme
    {
        SubdivisionScheme IImplicitTilingScheme.SubdivisionScheme => SubdivisionScheme.UniformGrid;
        
        public TemplatedUri Subtrees { get; }
        public Dimensions TileSize { get; }

        public UniformGrid(TemplatedUri subtrees, Dimensions tileSize)
        {
            Subtrees = subtrees;
            TileSize = tileSize;
        }
    }
}