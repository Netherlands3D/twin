using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class distanceLogger : MonoBehaviour
    {
        public Slider slider;
        float startDistance;
        bool started = false;
        // Start is called before the first frame update
        public  void Reset()
        {
            started = false;
        }
        void setStartDistance(float value)
        {
            slider.maxValue = 2f * value;
            slider.value = value;
            started = true;
            
        }

        public void ShowDistance(float value)
        {
            if (started==false)
            {
                setStartDistance(value);
                return;
            }
            float distance = 0.5f * value / startDistance;
            if (distance>1f)
            {
                distance = 1f;
            }
            slider.value = value;
        }
        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
