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
        private ulong sqrDistanceBeforeShifting = 100000000;

        public ulong SqrDistanceBeforeShifting => sqrDistanceBeforeShifting;

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
            sqrDistanceBeforeShifting = (ulong)distanceBeforeShifting * (ulong)distanceBeforeShifting;

            StartCoroutine(AttemptShift());
        }

        private ulong CalculateDistanceSquaredInXZPlane(Vector3 pos1, Vector3 pos2)
        {
            float deltaX = pos1.x - pos2.x;
            float deltaZ = pos1.z - pos2.z;
            float distanceSquared = (deltaX * deltaX) + (deltaZ * deltaZ);
            return (ulong)distanceSquared;
        }

        private IEnumerator AttemptShift() 
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                ulong squaredDistance = CalculateDistanceSquaredInXZPlane(mainShifter.position, transform.position);
                if (squaredDistance < sqrDistanceBeforeShifting)
                {
                    continue;
                }

                // get the Coordinate of the mainShifter in WGS84_LatLonHeight
                var mainShifterPosition = mainShifter.position;
                Coordinate mainShifterCoordinate = new Coordinate(mainShifterPosition);
                Coordinate mainShifterWGS = mainShifterCoordinate.Convert(CoordinateSystem.WGS84_LatLonHeight);

                // get the Coordinate of the Origin in WGS84_LatLonHeight
                Coordinate originCoordinateWGS = new Coordinate(transform.position).Convert(CoordinateSystem.WGS84_LatLonHeight);
                double originEllipsoidalHeight = originCoordinateWGS.height;

                // move the origin to the coordinate of the mainShifter, maintaining its current ellipsoidalHeight.
                // because the wgs84-ellipse roughly follows the surface of the earth, (in contrast to Earth-Center-Earth-Fixed, coordinateSystems,
                // where the height is the distance from the equatorial plane in thedirection of the north pole),
                // keeping the ellipsoidal height constant ensures that the vertical positions in unity don't become huge.
                Coordinate newOriginCoordinate = mainShifterWGS;
                newOriginCoordinate.height = originEllipsoidalHeight;

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
        /// <param name="originDestination"></param>
        public void MoveOriginTo(Coordinate originDestination)
        {
            Coordinate currentOrigin = new Coordinate(transform.position);

#if UNITY_EDITOR
            Coordinate currentOriginInRDNAP = new Coordinate(transform.position).Convert(CoordinateSystem.RDNAP);
            Coordinate originDestinationInRDNAP = originDestination.Convert(CoordinateSystem.RDNAP);
            if (LogShifts) Debug.Log($"Moving origin from {currentOriginInRDNAP.ToVector3()} (EPSG:7415) to {originDestinationInRDNAP.ToVector3()} (EPSG:7415)");
#endif

            onPreShift.Invoke(currentOrigin, originDestination);

            CoordinateSystems.SetOrigin(originDestination);

            // Shout to the world that the origin has changed to this coordinate
            onPostShift.Invoke(currentOrigin, originDestination);
        }
    }
}