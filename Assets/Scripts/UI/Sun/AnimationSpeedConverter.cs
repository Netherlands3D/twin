using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class AnimationSpeedConverter : MonoBehaviour
    {
        public enum SpeedUnits
        {
            SecondsPerSecond = 0,
            HoursPerSecond = 1
        }

        private Dictionary<SpeedUnits, float> multiplicationFactor = new Dictionary<SpeedUnits, float>()
        {
            { SpeedUnits.SecondsPerSecond, 1f }, // 1 second per second is the base unit
            { SpeedUnits.HoursPerSecond, 1f / 3600f } // 1 hour per second equals 1/3600 seconds per second
        };

        [field: SerializeField] public SpeedUnits OriginalUnits { get; set; } = SpeedUnits.SecondsPerSecond;
        [field: SerializeField] public SpeedUnits TargetUnits { get; set; } = SpeedUnits.SecondsPerSecond;

        public float ConvertSpeed(float value)
        {
            return ConvertSpeed(value, OriginalUnits, TargetUnits);
        }

        public float ConvertSpeed(float value, SpeedUnits originalUnits, SpeedUnits targetUnits)
        {
            float originalFactor = multiplicationFactor[originalUnits];
            float targetFactor = multiplicationFactor[targetUnits];

            float convertedValue = (value / originalFactor) * targetFactor;

            return convertedValue;
        }

        //todo: when dropdown changes, update input field value
        //todo: when pressing fast forward, change increments depending on units
        //todo: default project with new projectData fields
    }
}