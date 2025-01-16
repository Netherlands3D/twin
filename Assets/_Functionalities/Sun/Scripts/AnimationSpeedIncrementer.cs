using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.Sun
{
    public class AnimationSpeedIncrementer : MonoBehaviour
    {
        private AnimationSpeedConverter converter;
        private float inputFieldSpeed;
        [SerializeField] private int speedIndex = 0;
        [SerializeField] private List<float> animationSpeedSteps;

        public UnityEvent<float> SetNewAnimationSpeed;

        private void Awake()
        {
            converter = GetComponent<AnimationSpeedConverter>();
        }

        public void IncrementAnimationSpeed()
        {
            // Check if we need to increase the index
            if (inputFieldSpeed >= animationSpeedSteps[speedIndex] && speedIndex < animationSpeedSteps.Count - 1)
                speedIndex++;

            inputFieldSpeed = animationSpeedSteps[speedIndex];
            var convertedValue = converter.ConvertSpeed(inputFieldSpeed, converter.TargetUnits, AnimationSpeedConverter.SpeedUnits.SecondsPerSecond);
            SetNewAnimationSpeed.Invoke(convertedValue);
        }

        public void DecrementAnimationSpeed()
        {
            // Check if we need to increase the index
            if (inputFieldSpeed <= animationSpeedSteps[speedIndex] && speedIndex > 0)
                speedIndex--;

            inputFieldSpeed = animationSpeedSteps[speedIndex];
            var convertedValue = converter.ConvertSpeed(inputFieldSpeed, converter.TargetUnits, AnimationSpeedConverter.SpeedUnits.SecondsPerSecond);
            SetNewAnimationSpeed.Invoke(convertedValue);
        }

        public void UpdateSpeedIndex(float value)
        {
            inputFieldSpeed = value;
            speedIndex = FindClosestIndex(value);
        }

        public int FindClosestIndex(float value)
        {
            int closestIndex = 0;
            float smallestDifference = Math.Abs(animationSpeedSteps[0] - value);

            for (int i = 1; i < animationSpeedSteps.Count; i++)
            {
                float currentDifference = Math.Abs(animationSpeedSteps[i] - value);
                if (currentDifference < smallestDifference)
                {
                    smallestDifference = currentDifference;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }
    }
}