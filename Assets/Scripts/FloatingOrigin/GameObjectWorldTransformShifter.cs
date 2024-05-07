using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class GameObjectWorldTransformShifter : WorldTransformShifter
    {
        private WorldTransform worldTransform;

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            this.worldTransform = worldTransform;
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            // We can just recalculate the transform position based on the real world Coordinate.
            var newPosition = CoordinateConverter
                .ConvertTo(worldTransform.Coordinate, CoordinateSystem.Unity)
                .ToVector3();

#if UNITY_EDITOR
            if (worldTransform.Origin.LogShifts) Debug.Log($"<color=grey>{gameObject.name}: Shifting from {transform.position} to {newPosition}</color>");
#endif

            transform.position = newPosition;
            transform.hasChanged = false;
        }

        private void Update()
        {
            if (transform.hasChanged)
            {
                UpdateCoordinateBasedOnUnityTransform();
                transform.hasChanged = false;
            }
        }

        private void UpdateCoordinateBasedOnUnityTransform()
        {
            if(!this.worldTransform)
                return;

            var position = transform.position;
            this.worldTransform.Coordinate = CoordinateConverter.ConvertTo(
                new Coordinate(CoordinateSystem.Unity, position.x, position.y, position.z),this.worldTransform.ReferenceCoordinateSystem
            );
        }
    }
}