using System;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class BreakSlider : MonoBehaviour
    {
        private Slider slider;
        private flyingCamera camerascript;

        void Awake()
        {
            slider = GetComponent<Slider>();
        }

        private void Start()
        {
            camerascript = Camera.main.GetComponent<flyingCamera>();
            slider.minValue = 0;
            slider.maxValue = camerascript.maxBreakTimer;
            slider.value = camerascript.breakTimer;
        }

        void Update()
        {
            slider.value = camerascript.breakTimer;
        }
    }
}