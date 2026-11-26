using System;
using Netherlands3D.Coordinates;
using Netherlands3D.ProjectTime;
using UnityEngine;

namespace Netherlands3D
{
    public class SunTime : MonoBehaviour
    {
        private TimeService timeService;
        private float longitude;
        private float latitude;
        
        [Header("Settings")] [SerializeField] private Light sunDirectionalLight;
        private const int gizmoRayLength = 10000;

        private void Start()
        {
            timeService = Services.ServiceLocator.GetService<TimeService>();
            timeService.timeOfDayChanged.AddListener(RecalculateOrigin);
        }

        private void OnDestroy()
        {
            timeService.timeOfDayChanged.RemoveListener(RecalculateOrigin);
        }

        private void DetermineCurrentLocationFromOrigin()
        {
            var wgs84SceneCenter = CoordinateSystems.CoordinateAtUnityOrigin.Convert(CoordinateSystem.WGS84_LatLon);
            longitude = (float)wgs84SceneCenter.easting;
            latitude = (float)wgs84SceneCenter.northing;
        }

        private void SetDirection(DateTime time)
        {
            Vector3 angles = new Vector3();
            SunPosition.CalculateSunPosition(time, (double)latitude, (double)longitude, out double azi, out double alt);
            angles.x = (float)alt * Mathf.Rad2Deg;
            angles.y = (float)azi * Mathf.Rad2Deg;

            sunDirectionalLight.transform.localRotation = Quaternion.Euler(angles);
        }

        public void RecalculateOrigin()
        {
            RecalculateOrigin(timeService.Time);
        }

        //call this when the origin changes to recalculate the origin and set the sun position without calling the time change event
        public void RecalculateOrigin(DateTime time)
        {
            DetermineCurrentLocationFromOrigin();
            SetDirection(time);
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var position = this.transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(position, position - sunDirectionalLight.transform.forward * gizmoRayLength);
        }
#endif
    }
}
