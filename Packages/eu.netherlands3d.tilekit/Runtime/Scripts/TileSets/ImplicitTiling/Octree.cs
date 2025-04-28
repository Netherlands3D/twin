using JetBrains.Annotations;
using KindMen.Uxios;
using Unity.Collections;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    public struct Octree : IImplicitTilingScheme
    {
        SubdivisionScheme IImplicitTilingScheme.SubdivisionScheme => SubdivisionScheme.Octree;

        public int SubtreeLevels { get; }
        public int AvailableLevels { get; }

        private NativeText subtrees;
        [CanBeNull] public TemplatedUri Subtrees => subtrees != null ? new TemplatedUri(subtrees.ConvertToString()) : null;

        public Octree(NativeText subtrees, int subtreeLevels = 0, int availableLevels = 0)
        {
            this.subtrees = subtrees;
            SubtreeLevels = subtreeLevels;
            AvailableLevels = availableLevels;
        }
    }
}