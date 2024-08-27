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

        [SerializeField] private SpeedUnits originalUnits = SpeedUnits.SecondsPerSecond;
        [SerializeField] private SpeedUnits targetUnits;
        
        public UnityEvent<float> OnSpeedConverted;

        public void ConvertSpeed(float value)
        {
            ConvertSpeed(value, originalUnits, targetUnits);
        }
        
        public void ConvertSpeed(float value, SpeedUnits originalUnits, SpeedUnits targetUnits)
        {
            float originalFactor = multiplicationFactor[originalUnits];
            float targetFactor = multiplicationFactor[targetUnits];

            float convertedValue = (value / originalFactor) * targetFactor;

            OnSpeedConverted?.Invoke(convertedValue);
        }

        //for the ui dropdown
        public void SetOriginalUnits(int units)
        {
            originalUnits = (SpeedUnits)units;
        }
        
        //for the ui dropdown
        public void SetTargetUnits(int units)
        {
            targetUnits = (SpeedUnits)units;
        }
    }
}