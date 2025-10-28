using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Rendering;
using UnityEngine;

namespace Netherlands3D.CityJson
{
    public class CityJSONWorldTransformShifter : GameObjectWorldTransformShifter
    {
        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            base.ShiftTo(worldTransform, fromOrigin, toOrigin);
            foreach (var renderer in GetComponentsInChildren<BatchedMeshInstanceRenderer>())
            {
                renderer.RecalculateMatrices();
            }
        }
    }
}