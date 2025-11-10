using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using KindMen.Uxios;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Tilekit.TileSets.BoundingVolumes;
using Netherlands3D.Tilekit.TileSets.ImplicitTiling;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public struct TileSet
    {
        public Tile Root { get; }

        public TileSet(Tile root)
        {
            Root = root;
        }
    }
}

namespace Netherlands3D.Tilekit.TileSets.BoundingVolumes
{
    public readonly struct BoxBoundingVolume: IBoundingVolume
    {
        public double3 Center { get; }

        public double3 Size { get; }

        public BoxBoundingVolume(double3 center, double3 size)
        {
            Center = center;
            Size = size;
        }

        public BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }
    }
    
    public readonly struct RegionBoundingVolume : IBoundingVolume
    {
        private readonly double3 center;
        private readonly double3 size;

        public double3 Center => center;

        public double3 Size => size;

        public double West { get; }

        public double South { get; }

        public double East { get; }

        public double North { get; }

        public double MinHeight { get; }

        public double MaxHeight { get; }

        public RegionBoundingVolume(double west, double south, double east, double north, double minHeight, double maxHeight)
        {
            this.West = west;
            this.South = south;
            this.East = east;
            this.North = north;
            this.MinHeight = minHeight;
            this.MaxHeight = maxHeight;
            
            size = new double3(
                east - west,
                maxHeight - minHeight,
                north - south
            );
            
            center = new double3(
                west + size.x * .5d,
                minHeight + size.y * .5d,
                south + size.z * .5d
            );
        }
        
        public RegionBoundingVolume(BoundsDouble bounds) : this(
            bounds.Min.x, 
            bounds.Min.y, 
            bounds.Max.x, 
            bounds.Max.y, 
            bounds.Min.z, 
            bounds.Max.z
        ) {
        }
        
        public BoundsDouble ToBounds()
        {
            return new BoundsDouble(Center, Size);
        }
    }

    public readonly struct SphereBoundingVolume: IBoundingVolume
    {
        public double3 Center { get; }

        public double3 Size { get; }

        public SphereBoundingVolume(double3 center, double3 size)
        {
            Center = center;
            Size = size;
        }

        public BoundsDouble ToBounds()
        {
            throw new NotImplementedException();
        }
    }
}

namespace Netherlands3D.Tilekit.TileSets.ImplicitTiling
{
    public struct None : IImplicitTilingScheme
    {
        SubdivisionScheme IImplicitTilingScheme.SubdivisionScheme => SubdivisionScheme.None;

        private NativeText subtrees;
        [CanBeNull] public TemplatedUri Subtrees => subtrees != null ? new TemplatedUri(subtrees.ConvertToString()) : null;

        public None(NativeText subtreesUri)
        {
            subtrees = subtreesUri;
        }
    }
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
    
    public struct QuadTree : IImplicitTilingScheme
    {
        SubdivisionScheme IImplicitTilingScheme.SubdivisionScheme => SubdivisionScheme.Quadtree;

        public int SubtreeLevels { get; }
        public int AvailableLevels { get; }

        private NativeText subtrees;
        [CanBeNull] public TemplatedUri Subtrees => subtrees != null ? new TemplatedUri(subtrees.ConvertToString()) : null;

        public QuadTree(NativeText subtrees, int subtreeLevels = 0, int availableLevels = 0)
        {
            this.subtrees = subtrees;
            SubtreeLevels = subtreeLevels;
            AvailableLevels = availableLevels;
        }
    }
    
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

namespace Netherlands3D.Tilekit.TileSets
{
    public interface IBoundingVolume
    {
        public double3 Center { get; }
        public double3 Size { get; }

        public BoundsDouble ToBounds();
    }

    public enum TypesOfBoundingVolume : byte
    {
        Region,
        Sphere,
        Box
    }

    public readonly struct BoundingVolume : IBoundingVolume
    {
        public TypesOfBoundingVolume Type { get; }
        public RegionBoundingVolume Region { get; }
        public SphereBoundingVolume Sphere { get; }
        public BoxBoundingVolume Box { get; }

        public BoundingVolume(RegionBoundingVolume region) : this()
        {
            Type = TypesOfBoundingVolume.Region;
            Region = region;
        }
        
        public BoundingVolume(SphereBoundingVolume region) : this()
        {
            Type = TypesOfBoundingVolume.Sphere;
            Sphere = region;
        }
        
        public BoundingVolume(BoxBoundingVolume region) : this()
        {
            Type = TypesOfBoundingVolume.Box;
            Box = region;
        }

        public static BoundingVolume RegionBoundingVolume(double west, double south, double east, double north, double minHeight, double maxHeight)
        {
            return new BoundingVolume(new RegionBoundingVolume(west, south, east, north, minHeight, maxHeight));
        }

        public double3 Center
        {
            get
            {
                return Type switch
                {
                    TypesOfBoundingVolume.Region => Region.Center,
                    TypesOfBoundingVolume.Sphere => Sphere.Center,
                    TypesOfBoundingVolume.Box => Box.Center,
                    _ => throw new Exception("Invalid bounding volume, either Region or Sphere or Box should be provided.")
                };
            }
        }

        public double3 Size
        {
            get
            {
                return Type switch
                {
                    TypesOfBoundingVolume.Region => Region.Size,
                    TypesOfBoundingVolume.Sphere => Sphere.Size,
                    TypesOfBoundingVolume.Box => Box.Size,
                    _ => throw new Exception("Invalid bounding volume, either Region or Sphere or Box should be provided.")
                };
            }
        }

        public BoundsDouble ToBounds()
        {
            return Type switch
            {
                TypesOfBoundingVolume.Region => Region.ToBounds(),
                TypesOfBoundingVolume.Sphere => Sphere.ToBounds(),
                TypesOfBoundingVolume.Box => Box.ToBounds(),
                _ => throw new Exception("Invalid bounding volume, either Region or Sphere or Box should be provided.")
            };
        }
    }

    public interface IImplicitTilingScheme
    {
        public SubdivisionScheme SubdivisionScheme { get; }
        
        // TODO: TemplatedUri is a class, this breaks
        public TemplatedUri Subtrees { get; }
    }

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

    public class Tiles : HashSet<Tile>
    {
        
    }

    public struct Tile : IDisposable, IEquatable<Tile>
    {
        public NativeText Id;
        public BoundingVolume BoundingVolume;
        public double GeometricError;
        public NativeList<TileContent> TileContents;
        public Matrix4x4 Transform;
        public NativeList<Tile> Children;
        public ImplicitTilingScheme ImplicitTiling;
        public MethodOfRefinement Refine;


        public Tile(BoundingVolume boundingVolume, double geometricError = 10000) : this()
        {
            Id = new NativeText(Guid.NewGuid().ToString(), Allocator.Persistent);
            BoundingVolume = boundingVolume;
            GeometricError = geometricError;
            TileContents = new NativeList<TileContent>(Allocator.Persistent);
            Children = new NativeList<Tile>(Allocator.Persistent);
            ImplicitTiling = new ImplicitTilingScheme(new None());
            Refine = MethodOfRefinement.Replace;
            Transform = Matrix4x4.identity;
        }

        public void Dispose()
        {
            Id.Dispose();
            TileContents.Dispose();
            Children.Dispose();
        }

        public bool Equals(Tile other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is Tile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
    
    public class TileContents : List<TileContent>
    {
    }

    public struct TileContent
    {
        public NativeText UriTemplate;
        public BoundingVolume BoundingVolume;

        public TileContent(NativeText uriTemplate, BoundingVolume boundingVolume)
        {
            UriTemplate = uriTemplate;
            BoundingVolume = boundingVolume;
        }
    }

    public enum SubdivisionScheme : byte
    {
        None,
        UniformGrid,
        Quadtree,
        Octree
    }

    public enum MethodOfRefinement : byte
    {
        Add,
        Replace
    }

}