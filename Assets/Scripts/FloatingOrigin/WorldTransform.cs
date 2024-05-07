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

        public CoordinateSystem ReferenceCoordinateSystem => referenceCoordinateSystem;
        public Origin Origin => origin;

        public UnityEvent<WorldTransform, Coordinate> onPreShift = new();
        public UnityEvent<WorldTransform, Coordinate> onPostShift = new();


        private void Awake()
        {
            if (origin == null)
            {
                origin = FindObjectOfType<Origin>();
            }

            worldTransformShifter = GetComponent<WorldTransformShifter>();
            if (worldTransformShifter == null)
            {
                worldTransformShifter = gameObject.AddComponent<GameObjectWorldTransformShifter>();
            }

            // Pre-initialize the coordinates before using them
            Coordinate = new Coordinate(ReferenceCoordinateSystem, 0, 0, 0);
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
            origin.onPreShift.AddListener(PrepareToShift);
            origin.onPostShift.AddListener(ShiftTo);
        }

        private void OnDisable()
        {
            origin.onPreShift.RemoveListener(PrepareToShift);
            origin.onPostShift.RemoveListener(ShiftTo);
        }

        private void PrepareToShift(Coordinate fromOrigin, Coordinate toOrigin)
        {
            // Invoke Pre-shifting event first so that a listener might do some things before the shifter is invoked
            onPreShift.Invoke(this, Coordinate);
            
            worldTransformShifter.PrepareToShift(this, fromOrigin, toOrigin);
        }

        private void ShiftTo(Coordinate fromOrigin, Coordinate toOrigin)
        {
            worldTransformShifter.ShiftTo(this, fromOrigin, toOrigin);
            
            onPostShift.Invoke(this, Coordinate);
        }
    }
}