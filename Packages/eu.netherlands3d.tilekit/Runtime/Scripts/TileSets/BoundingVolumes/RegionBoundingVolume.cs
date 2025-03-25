using System.Runtime.Serialization;

namespace Netherlands3D.Tilekit.TileSets.BoundingVolumes
{
    [DataContract(Namespace = "eu.netherlands3d.tilekit.tilesets.bounding_volumes", Name = "RegionBoundingVolume")]
    public class RegionBoundingVolume : BoundingVolume
    {
        private readonly double west;
        private readonly double south;
        private readonly double east;
        private readonly double north;
        private readonly double minHeight;
        private readonly double maxHeight;

        private Vector3Double? cachedCenter = null;
        private Vector3Double? cachedSize = null;

        public override Vector3Double Center
        {
            get
            {
                if (cachedCenter == null)
                {
                    cachedCenter = new Vector3Double(
                        West + Size.x * .5d,
                        MinHeight + Size.y * .5d,
                        South + Size.z * .5d
                    );
                }

                return cachedCenter.Value;
            }
        }

        // Cache the Size property
        public override Vector3Double Size
        {
            get
            {
                if (cachedSize == null)
                {
                    cachedSize = new Vector3Double(
                        East - West,
                        MaxHeight - MinHeight,
                        North - South
                    );
                }

                return cachedSize.Value;
            }
        }

        public double West => west;

        public double South => south;

        public double East => east;

        public double North => north;

        public double MinHeight => minHeight;

        public double MaxHeight => maxHeight;

        public RegionBoundingVolume(double west, double south, double east, double north, double minHeight, double maxHeight)
        {
            this.west = west;
            this.south = south;
            this.east = east;
            this.north = north;
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
        }
        
        public RegionBoundingVolume(BoundsDouble bounds)
        {
            this.west = bounds.Min.x;
            this.south = bounds.Min.y;
            this.east = bounds.Max.x;
            this.north = bounds.Max.y;
            this.minHeight = bounds.Min.z;
            this.maxHeight = bounds.Max.z;
        }
        
        public override BoundsDouble ToBounds()
        {
            return new BoundsDouble(Center, Size);
        }
    }
}