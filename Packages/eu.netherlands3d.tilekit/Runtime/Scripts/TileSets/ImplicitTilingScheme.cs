using System;
using KindMen.Uxios;
using Netherlands3D.Tilekit.TileSets.BoundingVolumes;
using Netherlands3D.Tilekit.TileSets.ImplicitTiling;

namespace Netherlands3D.Tilekit.TileSets
{
    public struct ImplicitTilingScheme : IImplicitTilingScheme
    {
        public SubdivisionScheme SubdivisionScheme { get; }

        public TemplatedUri Subtrees
        {
            get
            {
                return SubdivisionScheme switch
                {
                    SubdivisionScheme.None => none.Subtrees,
                    SubdivisionScheme.Quadtree => quadTree.Subtrees,
                    SubdivisionScheme.Octree => octree.Subtrees,
                    SubdivisionScheme.UniformGrid => uniformGrid.Subtrees,
                    _ => throw new Exception()
                };
            }
        }

        private None none;
        private QuadTree quadTree;
        private Octree octree;
        private UniformGrid uniformGrid;
       
        public ImplicitTilingScheme(None none) : this()
        {
            this.none = none;
            SubdivisionScheme = SubdivisionScheme.None;
        }

        public ImplicitTilingScheme(QuadTree quadTree) : this()
        {
            this.quadTree = quadTree;
            SubdivisionScheme = SubdivisionScheme.Quadtree;
        }

        public ImplicitTilingScheme(Octree octree) : this()
        {
            this.octree = octree;
            SubdivisionScheme = SubdivisionScheme.Octree;
        }

        public ImplicitTilingScheme(UniformGrid uniformGrid) : this()
        {
            this.uniformGrid = uniformGrid;
            SubdivisionScheme = SubdivisionScheme.UniformGrid;
        }
    }
}