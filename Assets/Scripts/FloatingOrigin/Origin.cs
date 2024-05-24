using System;
using System.Collections;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class Origin : MonoBehaviour, IHasCoordinate
    {
        public Coordinate Coordinate { get; set; }
        public Transform mainShifter;
        public CoordinateSystem referenceCoordinateSystem;

        /// <summary>
        /// The maximum of 30.000 is to prevent integer overflows when squaring the distance, with
        /// a safety margin for when the camera/shifter moves really fast.
        /// </summary>
        [Tooltip("The distance from the mainCamera/mainShifter before shifting the Origin")]
        [Range(1000, 30000)]
        [SerializeField] private int distanceBeforeShifting = 10000;
        
        /// <summary>
        /// A cached square root of the distanceBeforeShifting value for performance optimization.
        /// </summary>
        private int sqrDistanceBeforeShifting = 100000000;

        public UnityEvent<Coordinate, Coordinate> onPreShift = new(); 
        public UnityEvent<Coordinate, Coordinate> onPostShift = new();

        public bool LogShifts = false;
        
        private void Start()
        {
            mainShifter = mainShifter == null ? Camera.main.transform : mainShifter;

            // Cache the square of the distance
            sqrDistanceBeforeShifting = distanceBeforeShifting * distanceBeforeShifting;

            Coordinate = new Coordinate(transform.position);

            StartCoroutine(AttemptShift());
        }

        private IEnumerator AttemptShift() 
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                var mainShifterPosition = mainShifter.position;
                //cancel out the elevation (Y-axis), elevation at the origin will not be changed, and we dont want the origin-moving to occur every frame when the camera is high above the origin.
                mainShifterPosition.y = transform.position.y;
                var distanceToCamera = mainShifterPosition - transform.position;

                // sqrMagnitude is used because it outperforms magnitude quite a bit: https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html
                if (distanceToCamera.sqrMagnitude < sqrDistanceBeforeShifting)
                {
                    continue;
                }

                Profiler.BeginSample("PerformOriginShift");
                var newPosition = new Vector3(mainShifterPosition.x, transform.position.y, mainShifterPosition.z);

                // Move this Origin to the camera's position, but keep the same height as it is now. 
                MoveOriginTo(new Coordinate(newPosition));

                Profiler.EndSample();
            }
        }

        public void MoveOriginTo(Coordinate destination)
        {
            var from  = Coordinate;
            var to = destination;
#if UNITY_EDITOR
            if (LogShifts) Debug.Log($"Moving origin from {from.ToVector3()} (EPSG:{from.CoordinateSystem}) to {to.ToVector3()} (EPSG:{to.CoordinateSystem})");
#endif

            onPreShift.Invoke(from, to);
            
            Coordinate = to;
            // TODO: Shouldn't this be a listener to the event below?
            CoordinateSystems.SetOrigin(Coordinate);
            // Shout to the world that the origin has changed to this coordinate
            onPostShift.Invoke(from, to);
        }
    }
}