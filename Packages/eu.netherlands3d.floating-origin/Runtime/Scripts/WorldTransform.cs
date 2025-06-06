using System;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class WorldTransform : MonoBehaviour, IHasCoordinate
    {
        [SerializeField] private WorldTransformShifter worldTransformShifter;
        public Coordinate Coordinate {
            get;
            set;
        }
        public Quaternion Rotation
        {
            get;
            set;
        }
        public CoordinateSystem ReferenceCoordinateSystem => CoordinateSystems.connectedCoordinateSystem;
        public Origin Origin => Origin.current;

        public UnityEvent<WorldTransform, Coordinate> onPreShift = new();
        public UnityEvent<WorldTransform, Coordinate> onPostShift = new();


        private bool shiftPrepared = false;

        private void Awake()
        {
            if (!TryGetComponent(out worldTransformShifter))
            {
                worldTransformShifter = gameObject.AddComponent<GameObjectWorldTransformShifter>();
            }
            UpdateCoordinateBasedOnUnityTransform();
        }

        private void OnEnable()
        {
            Origin.onPreShift.AddListener(PrepareToShift);
            Origin.onPostShift.AddListener(ShiftTo);

            if (shiftPrepared)
            {
                ShiftTo(Coordinate, Coordinate);
            }
            
        }

        private void OnDisable()
        {
            Origin.onPreShift.RemoveListener(PrepareToShift);
            Origin.onPostShift.RemoveListener(ShiftTo);
            //prepare for shifting, so we save the Coordinates. we can use these coordinates when the gameObject is Re-Enabled
            //so the geometrie will appear in the correct unity-position.
            PrepareToShift(Coordinate, Coordinate);
        }

        private void PrepareToShift(Coordinate fromOrigin, Coordinate toOrigin)
        {
            // Invoke Pre-shifting event first so that a listener might do some things before the shifter is invoked
            onPreShift.Invoke(this, Coordinate);
            
            worldTransformShifter.PrepareToShift(this, fromOrigin, toOrigin);
            shiftPrepared = true;
        }

        private void ShiftTo(Coordinate fromOrigin, Coordinate toOrigin)
        {
            worldTransformShifter.ShiftTo(this, fromOrigin, toOrigin);
            onPostShift.Invoke(this, Coordinate);
            shiftPrepared = false;
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
            Coordinate = new Coordinate(transform.position).Convert(ReferenceCoordinateSystem);
            Rotation = Quaternion.Inverse(Coordinate.RotationToLocalGravityUp()) * transform.rotation;
        }

        public void RecalculatePositionAndRotation()
        {
            transform.position = Coordinate.ToUnity();
            transform.rotation = Coordinate.RotationToLocalGravityUp() * Rotation;

        }
        
        // Set the coordinate and position directly
        public void MoveToCoordinate(Coordinate coordinate)
        {
            Coordinate = coordinate.Convert(ReferenceCoordinateSystem);
            transform.position = coordinate.ToUnity();
        }
        
        public void SetRotation(Quaternion rotation)
        {
            Rotation = rotation;
            transform.rotation = Coordinate.RotationToLocalGravityUp() * rotation;
        }
    }
}