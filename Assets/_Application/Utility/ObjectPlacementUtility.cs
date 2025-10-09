using Netherlands3D.Twin.Samplers;
using UnityEngine;

namespace Netherlands3D.Twin.Utility
{
    public static class ObjectPlacementUtility
    {
        public static Vector3 GetSpawnPoint()
        {
            return GameObject.FindAnyObjectByType<PointerToWorldPosition>().GetWorldPointCenterView();
        }
    }
}
