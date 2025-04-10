using KindMen.Uxios;

namespace Netherlands3D.Tilekit.TileSets
{
    public interface IImplicitTilingScheme
    {
        protected SubdivisionScheme SubdivisionScheme { get; }
        
        // TODO: TemplatedUri is a class, this breaks
        public TemplatedUri Subtrees { get; }
    }
}