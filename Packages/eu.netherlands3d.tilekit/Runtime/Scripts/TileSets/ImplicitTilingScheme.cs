using KindMen.Uxios;

namespace Netherlands3D.Tilekit.TileSets
{
    public abstract class ImplicitTilingScheme
    {
        protected virtual SubdivisionScheme SubdivisionScheme { get; } = SubdivisionScheme.Quadtree;
        public TemplatedUri Subtrees;
    }
}