using System.Runtime.Serialization;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.TileSets.BoundingVolumes
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.bounding_volumes", Name = "RegionBoundingVolume")]
    public struct RegionBoundingVolume : IBoundingVolume
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
}