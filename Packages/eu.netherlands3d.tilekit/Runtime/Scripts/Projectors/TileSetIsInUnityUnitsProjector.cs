using UnityEngine;

namespace Netherlands3D.Tilekit.Projectors
{
    [CreateAssetMenu(menuName = "Netherlands3D/Tilekit/Create Default Projector", fileName = "TileSet is in Unity Units", order = 0)]
    public class TileSetIsInUnityUnitsProjector : Projector
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