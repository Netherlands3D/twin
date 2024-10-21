using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Indicators.UI
{
    public class Loader : MonoBehaviour
    {
        public Slider slider;
        public float speed = 1f;

        private void OnEnable()
        {
            slider.value = 0f;
        }

        private void Update()
        {
            if (slider.value < slider.maxValue)
            {
                slider.value += speed * Time.deltaTime;
            }
            else
            {
                slider.value = 0f;
            }
        }
    }
}
