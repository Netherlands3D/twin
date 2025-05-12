using System;
using Netherlands3D.Tilekit.TileSets.BoundingVolumes;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.TileSets
{
    public struct BoundingVolume : IBoundingVolume
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
}