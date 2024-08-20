using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class GameObjectWorldTransformShifter : WorldTransformShifter
    {
        private WorldTransform worldTransform;

        private void Start()
        {
            worldTransform = transform.GetComponent<WorldTransform>();

            UpdateCoordinateBasedOnUnityTransform();
        }

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            // Always update the coordinate based on the Unity transform before shifting to make sure the coordinate is up to date.
            UpdateCoordinateBasedOnUnityTransform();
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            // We can just recalculate the transform position based on the real world Coordinate.
            var newPosition = worldTransform.Coordinate.ToUnity();
            // And recalculate the transform rotation based on the ral world Rotation.
            var newRotation = worldTransform.Coordinate.RotationToLocalGravityUp() * worldTransform.Rotation;

#if UNITY_EDITOR
            if (worldTransform.Origin.LogShifts) Debug.Log($"<color=grey>{gameObject.name}: Shifting from {transform.position} to {newPosition}</color>");
#endif

            transform.position = newPosition;
            transform.rotation = newRotation;
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
            if (!this.worldTransform) return;

            this.worldTransform.Coordinate = new Coordinate(transform.position)
                .Convert(this.worldTransform.ReferenceCoordinateSystem);
            var rotationToLocalGravityUp = this.worldTransform.Coordinate.RotationToLocalGravityUp();
            this.worldTransform.Rotation = Quaternion.Inverse(rotationToLocalGravityUp) * transform.rotation;
        }
    }
}