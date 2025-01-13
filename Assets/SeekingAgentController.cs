using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;
using UnitySteer.Behaviors;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(SteerForPoint))]
    [RequireComponent(typeof(WorldTransform))]
    public class SeekingAgentController : MonoBehaviour
    {
        public CoordinateSystem CoordinateSystem = CoordinateSystem.RD;
        public Vector3Double Destination = new Vector3Double();
        private Vector3 cachedUnityDestination;
        private Coordinate cachedDestinationCoordinate;
        private SteerForPoint steerForPoint;
        private WorldTransform worldTransform;
        public WorldTransform Passenger;

        private void Awake()
        {
            steerForPoint = GetComponent<SteerForPoint>();
            worldTransform = GetComponent<WorldTransform>();
        }

        private void OnEnable()
        {
            worldTransform.onPostShift.AddListener(OnShift);
        }

        private void OnDisable()
        {
            worldTransform.onPostShift.RemoveListener(OnShift);
            Stop();
        }

        /// <summary>
        /// Parameters are not used because the Coordinate represents the current world transform's coordinate,
        /// but we need to update the destination.
        /// </summary>
        private void OnShift(WorldTransform _1, Coordinate _2)
        {
            UpdateDestination();
        }

        private void Update()
        {
            // While steering, we take the passenger along so that it travels
            if (steerForPoint.enabled)
            {
                Passenger.transform.position = worldTransform.transform.position;
            }
            // While not steering, we stick with the passenger so that when we start steering it won't jump around
            else
            {
                worldTransform.transform.position = Passenger.transform.position;
            }
        }

        private void UpdateDestination()
        {
            cachedUnityDestination = cachedDestinationCoordinate.ToUnity();
            steerForPoint.TargetPoint = cachedUnityDestination;
        }

        /// <summary>
        /// Seeks the destination provided in this component's settings 
        /// </summary>
        [ContextMenu("Start seeking")]
        public void SeekTo()
        {
            SeekTo(new Coordinate(CoordinateSystem, Destination.x, Destination.y, Destination.z));
        }

        /// <summary>
        /// Seeks the given coordinates
        /// </summary>
        public void SeekTo(Coordinate coordinate)
        {
            cachedDestinationCoordinate = coordinate;
            UpdateDestination();
            steerForPoint.enabled = true;
        }

        /// <summary>
        /// Seeks the provided real world object 
        /// </summary>
        public void SeekTo(WorldTransform worldTransform)
        {
            SeekTo(worldTransform.Coordinate);
        }

        [ContextMenu("Stop seeking")]
        public void Stop()
        {
            steerForPoint.enabled = false;
        }
    }
}
