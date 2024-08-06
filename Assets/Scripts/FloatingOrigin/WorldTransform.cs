using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class WorldTransform : MonoBehaviour, IHasCoordinate
    {
        [SerializeField] private WorldTransformShifter worldTransformShifter;
        [SerializeField] private CoordinateSystem referenceCoordinateSystem = CoordinateSystem.RDNAP;
        public Coordinate Coordinate {
            get;
            set;
        }
        public Quaternion Rotation
        {
            get;
            set;
        }
        public CoordinateSystem ReferenceCoordinateSystem => referenceCoordinateSystem;
        public Origin Origin => Origin.current;

        public UnityEvent<WorldTransform, Coordinate> onPreShift = new();
        public UnityEvent<WorldTransform, Coordinate> onPostShift = new();

        private void Awake()
        {
            worldTransformShifter = GetComponent<WorldTransformShifter>();
            if (worldTransformShifter == null)
            {
                worldTransformShifter = gameObject.AddComponent<GameObjectWorldTransformShifter>();
            }
            Coordinate = new Coordinate(referenceCoordinateSystem, 0, 0, 0);
        }

        private void OnValidate()
        {
            if (referenceCoordinateSystem == CoordinateSystem.Unity)
            {
                Debug.LogError(
                    "Reference coordinate system for a World Transform cannot be in Unity coordinates; "+
                    "otherwise the Origin's location won't be taken into account."
                );
                referenceCoordinateSystem = CoordinateSystem.WGS84_LatLonHeight;
            }
        }

        private void OnEnable()
        {
            Origin.onPreShift.AddListener(PrepareToShift);
            Origin.onPostShift.AddListener(ShiftTo);
        }

        private void OnDisable()
        {
            Origin.onPreShift.RemoveListener(PrepareToShift);
            Origin.onPostShift.RemoveListener(ShiftTo);
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