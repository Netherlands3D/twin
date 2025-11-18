using Netherlands3D.Coordinates;
using Netherlands3D.Tilekit.Geometry;
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

    public static class BoundsDoubleExtensions
    {
        public static Bounds ToLocalCoordinateSystem(this BoundsDouble bounds, CoordinateSystem coordinateSystem)
        {
            var result = new Bounds();
            result.SetMinMax(
                new Coordinate(coordinateSystem, bounds.Min.x, bounds.Min.y, bounds.Min.z).ToUnity(),
                new Coordinate(coordinateSystem, bounds.Max.x, bounds.Max.y, bounds.Max.z).ToUnity()
            );
            return result;
        }
    }
}