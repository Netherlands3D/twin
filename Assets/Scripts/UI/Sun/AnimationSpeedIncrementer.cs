using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin
{
    public class AnimationSpeedIncrementer : MonoBehaviour
    {
        private AnimationSpeedConverter converter;
        public UnityEvent<float> SetNewAnimationSpeed;
        [SerializeField] private int speedIndex = 0;
        [SerializeField] private List<float> animationSpeedSteps;

        private void Awake()
        {
            converter = GetComponent<AnimationSpeedConverter>();
        }

        public void IncrementAnimationSpeed()
        {
            speedIndex = Math.Clamp(speedIndex + 1, 0, animationSpeedSteps.Count - 1);
            var convertedValue = converter.ConvertSpeed(animationSpeedSteps[speedIndex], converter.TargetUnits, AnimationSpeedConverter.SpeedUnits.SecondsPerSecond);
            SetNewAnimationSpeed.Invoke(convertedValue);
        }

        public void DecrementAnimationSpeed()
        {
            speedIndex = Math.Clamp(speedIndex - 1, 0, animationSpeedSteps.Count - 1);
            var convertedValue = converter.ConvertSpeed(animationSpeedSteps[speedIndex], converter.TargetUnits, AnimationSpeedConverter.SpeedUnits.SecondsPerSecond);
            SetNewAnimationSpeed.Invoke(convertedValue);
        }
        
        // public int FindClosestIndex(float value)
        // {
        //     int closestIndex = 0;
        //     float smallestDifference = Math.Abs(animationSpeedSteps[0] - value);
        //
        //     for (int i = 1; i < animationSpeedSteps.Count; i++)
        //     {
        //         float currentDifference = Math.Abs(animationSpeedSteps[i] - value);
        //         if (currentDifference < smallestDifference)
        //         {
        //             smallestDifference = currentDifference;
        //             closestIndex = i;
        //         }
        //     }
        //
        //     return closestIndex;
        // }
    }
}