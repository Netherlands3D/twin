using UnityEngine;

namespace Netherlands3D.Twin.ExtensionMethods
{
    public static class GameObjectExtensions
    {
        public static bool IsInLayerMask(this GameObject obj, LayerMask mask)
        {
            return ((mask.value & (1 << obj.layer)) > 0);
        }
    }
}
