namespace Netherlands3D.Tilekit.TileSets
{
    public abstract class BoundingVolume
    {
        public virtual Vector3Double Center { get; }
        public virtual Vector3Double Size { get; }
        public abstract BoundsDouble ToBounds();
    }
}