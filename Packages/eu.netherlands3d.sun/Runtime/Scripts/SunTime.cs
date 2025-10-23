/*
 *  Copyright (C) X Gemeente
 *                X Amsterdam
 *                X Economic Services Departments
 *
 *  Licensed under the EUPL, Version 1.2 or later (the "License");
 *  You may not use this work except in compliance with the License.
 *  You may obtain a copy of the License at:
 *
 *    https://github.com/Amsterdam/Netherlands3D/blob/main/LICENSE.txt
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" basis,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
 *  implied. See the License for the specific language governing
 *  permissions and limitations under the License.
 */

using System;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Sun
{
    [ExecuteInEditMode]
    public class SunTime : MonoBehaviour
    {
        [Header("Time")] [SerializeField] private DateTimeKind dateTimeKind = DateTimeKind.Local;

        [FormerlySerializedAs("jumpToCurrentTimeAtStart")] [SerializeField]
        private bool useCurrentTime = false;

        [SerializeField] [Range(0, 24)] private int hour = 18;
        [SerializeField] [Range(0, 60)] private int minutes = 0;
        [SerializeField] [Range(0, 60)] private int seconds = 0;
        [SerializeField] [Range(1, 31)] private int day = 13;
        [SerializeField] [Range(1, 12)] private int month = 8;
        [SerializeField] [Range(1, 9999)] private int year = 2022;

        [Header("Settings")] [SerializeField] private Light sunDirectionalLight;
        [SerializeField] private bool animate = true;
        [SerializeField] private float timeSpeed = 1;
        [SerializeField] private int frameSteps = 1;

        [Header("Events")] public UnityEvent<DateTime> timeOfDayChanged = new();
        public UnityEvent<float> timeSpeedChanged = new();
        public UnityEvent<bool> useCurrentTimeChanged = new();
        public UnityEvent<bool> isAnimatingChanged = new();

        private float longitude;
        private float latitude;
        private DateTime time;

        public DateTime Time
        {
            get => time;
            set
            {
                if (time == value)
                    return;

                time = value;
                UpdateTimeOfDayPartsFromTime();
                SetDirection();
                timeOfDayChanged.Invoke(time);
            }
        }

        public bool UseCurrentTime
        {
            get => useCurrentTime;
            set
            {
                useCurrentTime = value;
                if (value)
                    ResetToNow();

                useCurrentTimeChanged.Invoke(value);
            }
        }

        public bool IsAnimating => animate;

        private int frameStep;
        private const int gizmoRayLength = 10000;

        private void Start()
        {
            if (useCurrentTime)
            {
                ResetToNow();
            }
            else
            {
                var newTime = new DateTime(year, month, day, hour, minutes, seconds, dateTimeKind);
                Time = newTime;
            }

            RecalculateOrigin();
        }

        private void Update()
        {
            if (!animate) return;

            Time = time.AddSeconds(timeSpeed * UnityEngine.Time.deltaTime);
            frameStep = (frameStep + 1) % frameSteps;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var position = this.transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(position, position - sunDirectionalLight.transform.forward * gizmoRayLength);
        }
#endif

        public void ToggleAnimation(bool animate)
        {
            this.animate = animate;
            isAnimatingChanged.Invoke(animate);
        }

        [Obsolete("Use the Time property instead")]
        public DateTime GetTime()
        {
            return time;
        }

        //function for in the inspector
        public void SetTime(DateTime time)
        {
            UseCurrentTime = false;
            Time = time;
        }

        public void SetTime(int hour, int minute, int second)
        {
            UseCurrentTime = false;

            hour = Mathf.Clamp(hour, 0, 24);
            minute = Mathf.Clamp(minute, 0, 60);
            second = Mathf.Clamp(second, 0, 60);

            Time = new DateTime(
                Time.Year,
                Time.Month,
                Time.Day,
                hour,
                minute,
                second,
                Time.Millisecond,
                Time.Kind);
        }

        public void SetHour(int hour)
        {
            SetTime(hour, Time.Minute, Time.Second);
        }

        public void SetMinutes(int minute)
        {
            SetTime(Time.Hour, minute, Time.Second);
        }

        public void SetSeconds(int second)
        {
            SetTime(Time.Hour, Time.Minute, second);
        }

        public void SetDate(int day, int month, int year)
        {
            UseCurrentTime = false;

            year = Mathf.Clamp(year, 1, 9999);
            month = Mathf.Clamp(month, 1, 12);
            var maxDay = DateTime.DaysInMonth(year, month);
            day = Mathf.Clamp(day, 1, maxDay);

            Time = new DateTime(
                year,
                month,
                day,
                Time.Hour,
                Time.Minute,
                Time.Second,
                Time.Millisecond,
                Time.Kind);
        }

        public void SetDay(int day)
        {
            SetDate(day, Time.Month, Time.Year);
        }

        public void SetMonth(int month)
        {
            SetDate(Time.Day, month, Time.Year);
        }

        public void SetYear(int year)
        {
            SetDate(Time.Day, Time.Month, year);
        }

        public void SetLocation(float longitude, float latitude)
        {
            this.longitude = longitude;
            this.latitude = latitude;

            SetDirection();
        }

        public void SetUpdateSteps(int i)
        {
            frameSteps = i;
        }

        public void MultiplyTimeSpeed(float multiplicationFactor)
        {
            timeSpeed = Math.Clamp(timeSpeed * multiplicationFactor, 1, 43200);
            timeSpeedChanged.Invoke(timeSpeed);
        }

        public void SetTimeSpeed(float speed)
        {
            timeSpeed = Math.Clamp(speed, 1, 43200);
            timeSpeedChanged.Invoke(timeSpeed);
        }

        public void ResetToNow()
        {
            useCurrentTime = true;
            useCurrentTimeChanged.Invoke(useCurrentTime);
            Time = DateTime.Now;
        }

        private void UpdateTimeOfDayPartsFromTime()
        {
            hour = time.Hour;
            minutes = time.Minute;
            seconds = time.Second;
            day = time.Day;
            month = time.Month;
            year = time.Year;
        }

        private void DetermineCurrentLocationFromOrigin()
        {
            var wgs84SceneCenter = CoordinateSystems.CoordinateAtUnityOrigin.Convert(CoordinateSystem.WGS84_LatLon);
            longitude = (float)wgs84SceneCenter.easting;
            latitude = (float)wgs84SceneCenter.northing;
        }

        private void SetDirection()
        {
            Vector3 angles = new Vector3();
            SunPosition.CalculateSunPosition(time, (double)latitude, (double)longitude, out double azi, out double alt);
            angles.x = (float)alt * Mathf.Rad2Deg;
            angles.y = (float)azi * Mathf.Rad2Deg;

            sunDirectionalLight.transform.localRotation = Quaternion.Euler(angles);
        }

        //call this when the origin changes to recalculate the origin and set the sun position without calling the time change event
        public void RecalculateOrigin()
        {
            DetermineCurrentLocationFromOrigin();
            SetDirection();
        }
    }
}