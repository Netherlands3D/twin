using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public enum QualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
    
    public class QualitySettings : MonoBehaviour
    {
        [SerializeField] private Toggle lowQualityToggle;
        [SerializeField] private Toggle mediumQualityToggle;
        [SerializeField] private Toggle highQualityToggle;

        private void OnEnable()
        {
            lowQualityToggle.onValueChanged.AddListener(SetGraphicsQualityToLow);
            mediumQualityToggle.onValueChanged.AddListener(SetGraphicsQualityToMedium);
            highQualityToggle.onValueChanged.AddListener(SetGraphicsQualityToHigh);
        }

        private void OnDisable()
        {
            lowQualityToggle.onValueChanged.RemoveListener(SetGraphicsQualityToLow);
            mediumQualityToggle.onValueChanged.RemoveListener(SetGraphicsQualityToMedium);
            highQualityToggle.onValueChanged.RemoveListener(SetGraphicsQualityToHigh);
        }

        private void SetGraphicsQualityToLow(bool isOn)
        {
            if(isOn)
                SetGraphicsQuality(QualityLevel.Low);
        }
        
        private void SetGraphicsQualityToMedium(bool isOn)
        {
            if(isOn)
                SetGraphicsQuality(QualityLevel.Medium);
        }
        
        private void SetGraphicsQualityToHigh(bool isOn)
        {
            if(isOn)
                SetGraphicsQuality(QualityLevel.High);
        }


        public static void SetGraphicsQuality(QualityLevel level)
        {
            print("setting ql to : " + level);
            UnityEngine.QualitySettings.SetQualityLevel((int)level);
        }
    }
}