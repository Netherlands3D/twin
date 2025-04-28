using KindMen.Uxios;
using Unity.Collections;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    public struct UniformGrid : IImplicitTilingScheme
    {
        SubdivisionScheme IImplicitTilingScheme.SubdivisionScheme => SubdivisionScheme.UniformGrid;
        
        private NativeText subtrees;
        public TemplatedUri Subtrees => subtrees != null ? new TemplatedUri(subtrees.ConvertToString()) : null;
        public Dimensions TileSize { get; }

        public UniformGrid(NativeText subtrees, Dimensions tileSize)
        {
            this.subtrees = subtrees;
            TileSize = tileSize;
        }
    }
}