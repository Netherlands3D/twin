using UnityEngine;

namespace Netherlands3D.Tilekit.Projectors
{
    [CreateAssetMenu(menuName = "Tilekit/Projectors/Unity Units", fileName = "TileSetIsInUnityUnitsProjector", order = 0)]
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