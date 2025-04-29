namespace Netherlands3D.Tilekit.Projectors
{
    public class TileSetIsInUnityUnitsProjector : BaseProjector
    {
        public override float ToUnityUnits(double original)
        {
            return (float)original;
        }

        public override double FromUnityUnits(float original)
        {
            return original;
        }
    }
}