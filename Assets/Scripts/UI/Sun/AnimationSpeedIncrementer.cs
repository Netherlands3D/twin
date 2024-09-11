using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Twin
{
    public class AnimationSpeedIncrementer : MonoBehaviour
    {
        private AnimationSpeedConverter converter;
        private float inputFieldSpeed;
        private TMP_InputField inputField;
        [SerializeField] private int speedIndex = 0;
        [SerializeField] private List<float> animationSpeedSteps;
        [SerializeField] private string realtimeSpeedText;
        public UnityEvent<float> SetNewAnimationSpeed;

        private void Awake()
        {
            converter = GetComponent<AnimationSpeedConverter>();
            inputField = GetComponent<TMP_InputField>();
        }

        private void Start()
        {
            SetToRealTime(); //set initial values
        }

        public void IncrementAnimationSpeed()
        {
            // Check if we need to increase the index
            if (inputFieldSpeed >= animationSpeedSteps[speedIndex] && speedIndex < animationSpeedSteps.Count - 1)
                speedIndex++;

            if (speedIndex == 0)
            {
                SetToRealTime();
                return;
            }

            inputField.interactable = true;

            inputFieldSpeed = animationSpeedSteps[speedIndex];
            var convertedValue = converter.ConvertSpeed(inputFieldSpeed, converter.TargetUnits, AnimationSpeedConverter.SpeedUnits.SecondsPerSecond);
            SetNewAnimationSpeed.Invoke(convertedValue);
        }

        private void SetToRealTime()
        {
            inputFieldSpeed = 1 / 3600f;
            SetNewAnimationSpeed.Invoke(1);
            inputField.text = realtimeSpeedText; //infinity symbol
            inputField.interactable = false;
        }

        public void DecrementAnimationSpeed()
        {
            // Check if we need to increase the index
            if (inputFieldSpeed <= animationSpeedSteps[speedIndex] && speedIndex > 0)
                speedIndex--;
            
            if (speedIndex == 0)
            {
                SetToRealTime();
                return;
            }

            inputField.interactable = true;

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