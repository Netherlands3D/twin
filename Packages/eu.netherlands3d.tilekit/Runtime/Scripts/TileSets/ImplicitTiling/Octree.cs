using System.Runtime.Serialization;
using JetBrains.Annotations;
using KindMen.Uxios;

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.implicit_tiling", Name = "Octree")]
    public struct Octree : IImplicitTilingScheme
    {
        SubdivisionScheme IImplicitTilingScheme.SubdivisionScheme => SubdivisionScheme.Octree;

        public int SubtreeLevels { get; }
        public int AvailableLevels { get; }

        [CanBeNull] public TemplatedUri Subtrees { get; }

        public Octree(TemplatedUri subtrees, int subtreeLevels = 0, int availableLevels = 0)
        {
            Subtrees = subtrees;
            SubtreeLevels = subtreeLevels;
            AvailableLevels = availableLevels;
        }
    }
}