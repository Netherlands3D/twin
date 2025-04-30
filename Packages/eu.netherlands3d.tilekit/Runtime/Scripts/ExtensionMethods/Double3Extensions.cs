using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.ExtensionMethods
{
    public static class Double3Extensions
    {
        public static Vector3 ToVector3(this double3 double3)
        {
            return new Vector3((float)double3.x, (float)double3.y, (float)double3.z);
        }
    }
}