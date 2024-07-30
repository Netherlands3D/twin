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

        public static Origin current;
        public Transform mainShifter;

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
        
        private void Awake()
        {
            current = this;
        }

        private void Start()
        {
            mainShifter = mainShifter == null ? Camera.main.transform : mainShifter;

            // Cache the square of the distance
            sqrDistanceBeforeShifting = distanceBeforeShifting * distanceBeforeShifting;

            StartCoroutine(AttemptShift());
        }

        private IEnumerator AttemptShift() 
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                if (((mainShifter.position.x-transform.position.x)* (mainShifter.position.x - transform.position.x))+ ((mainShifter.position.z - transform.position.z) * (mainShifter.position.z - transform.position.z))< sqrDistanceBeforeShifting)
                {
                    continue;
                }


                var mainShifterPosition = mainShifter.position;
                Coordinate mainShifterCoordinate = new Coordinate(mainShifterPosition);
                Coordinate mainShifterWGS = mainShifterCoordinate.Convert(CoordinateSystem.WGS84_LatLonHeight);

                Coordinate originCoordinateWGS = new Coordinate(transform.position).Convert(CoordinateSystem.WGS84_LatLonHeight);

                Coordinate newOriginCoordinate = mainShifterWGS;
                newOriginCoordinate.height = originCoordinateWGS.height;

                Profiler.BeginSample("PerformOriginShift");

                // Move this Origin to the camera's position, but keep the same height as it is now. 
                MoveOriginTo(newOriginCoordinate);

                Profiler.EndSample();
            }
        }

        /// <summary>
        /// Move the origin of the world to a new position and inform anyone interested this has happened and can
        /// take action, such as moving that object or changing matrices.
        ///
        /// By design, this behaviour -and specifically this method- does not change the position of any object
        /// (including the main shifter) so that there is a clear and distinct separation of responsibilities when
        /// it comes to moving the world's origin, and moving spatial (game) objects.
        /// </summary>
        /// <param name="destination"></param>
        public void MoveOriginTo(Coordinate destination)
        {
            Coordinate CurrentOrigin = new Coordinate(transform.position);

#if UNITY_EDITOR
            Coordinate mainShifterRD = new Coordinate(transform.position).Convert(CoordinateSystem.RDNAP);
            Coordinate destinationRD = destination.Convert(CoordinateSystem.RDNAP);
            if (LogShifts) Debug.Log($"Moving origin from {mainShifterRD.ToVector3()} (EPSG:7415) to {destinationRD.ToVector3()} (EPSG:7415)");
#endif

            onPreShift.Invoke(CurrentOrigin, destination);

            CoordinateSystems.SetOrigin(destination);

            // Shout to the world that the origin has changed to this coordinate
            onPostShift.Invoke(CurrentOrigin, destination);
        }
    }
}