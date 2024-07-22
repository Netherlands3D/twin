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

            StartCoroutine(AttemptShift());
        }

        private IEnumerator AttemptShift() 
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                var mainShifterPosition = mainShifter.position;
                var originPosition = transform.position;
                var newPosition = new Vector3(mainShifterPosition.x, originPosition.y, mainShifterPosition.z);
                
                // We use newPosition instead of mainShifterPosition so that the Y component is the same and we 
                // calculate the distance on a flat plane. This prevents origin shifts when the only change of the
                // shifter is the height.
                var distanceBetweenMainShifterAndOrigin = newPosition - originPosition;

                // sqrMagnitude is used because it outperforms magnitude quite a bit: https://docs.unity3d.com/ScriptReference/Vector3-sqrMagnitude.html
                if (distanceBetweenMainShifterAndOrigin.sqrMagnitude < sqrDistanceBeforeShifting)
                {
                    continue;
                }

                Profiler.BeginSample("PerformOriginShift");

                // Move this Origin to the camera's position, but keep the same height as it is now. 
                MoveOriginTo(new Coordinate(newPosition));

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
            Coordinate mainShifterRD = new Coordinate(mainShifter.position).Convert(CoordinateSystem.RDNAP);
           
#if UNITY_EDITOR
            Coordinate destinationRD = destination.Convert(CoordinateSystem.RDNAP);
            if (LogShifts) Debug.Log($"Moving origin from {mainShifterRD.ToVector3()} (EPSG:7415) to {destinationRD.ToVector3()} (EPSG:7415)");
#endif

            onPreShift.Invoke(mainShifterRD, destination);

            CoordinateSystems.SetOrigin(destination);

            // Shout to the world that the origin has changed to this coordinate
            onPostShift.Invoke(mainShifterRD, destination);
        }
    }
}