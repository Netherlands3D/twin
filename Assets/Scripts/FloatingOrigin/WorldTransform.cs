using System;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class WorldTransform : MonoBehaviour, IHasCoordinate
    {
        [SerializeField] private Origin origin;
        [SerializeField] private WorldTransformShifter worldTransformShifter;
        [SerializeField] private CoordinateSystem referenceCoordinateSystem = CoordinateSystem.WGS84;
        public Coordinate Coordinate {
            get;
            set;
        }

        /// <summary>
        /// Called when the origin shifts with the from and to of the origin, because
        /// of the custom shifters in this class we cannot be sure this transform itself
        /// changed position.
        /// </summary>
        public UnityEvent<Coordinate, Coordinate> onShift = new();

        private void Awake()
        {
            if (origin == null)
            {
                origin = FindObjectOfType<Origin>();
            }
        }

        private void OnValidate()
        {
            if (referenceCoordinateSystem == CoordinateSystem.Unity)
            {
                Debug.LogError(
                    "Reference coordinate system for a World Transform cannot be in Unity coordinates; "+
                    "otherwise the Origin's location won't be taken into account."
                );
                referenceCoordinateSystem = CoordinateSystem.WGS84;
            }
        }

        private void OnEnable()
        {
            var position = transform.position;
            Coordinate = CoordinateConverter.ConvertTo(
                new Coordinate(CoordinateSystem.Unity, position.x, position.y, position.z), 
                referenceCoordinateSystem
            ); 
            origin.onShiftOriginTo.AddListener(ShiftTo);
        }

        private void OnDisable()
        {
            origin.onShiftOriginTo.RemoveListener(ShiftTo);
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
            var position = transform.position;
            Coordinate = CoordinateConverter.ConvertTo(
                new Coordinate(CoordinateSystem.Unity, position.x, position.y, position.z), 
                Coordinate.CoordinateSystem
            );
        }

        private void ShiftTo(Coordinate from, Coordinate to)
        {
            if (worldTransformShifter == null)
            {
                // We can just recalculate the transform position based on the real world Coordinate.
                transform.position = CoordinateConverter.ConvertTo(Coordinate, CoordinateSystem.Unity).ToVector3();
            }
            else
            {
                worldTransformShifter.ShiftTo(this, from, to);
            }
            
            onShift.Invoke(from, to);
        }
    }
}