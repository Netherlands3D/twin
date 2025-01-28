using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class FPSCounter : MonoBehaviour
    {
        public float updateInterval = 0.25f;

        //using unscaledDT because we wont want the frame time affected by future effects (pausing/slowmo etc)
        private int fps = 0;        
        private float accumulatedFPS = 0f;
        private int frameCount = 0;
        private float timeElapsed = 0f;
        private TextMeshProUGUI fpsText;

        //since fps wont be a crazy number easily we can cache these strings to prevent any allocation impact
        private Dictionary<int, string> cachedStrings = new Dictionary<int, string>();

        private void Start()
        {
            fpsText = GetComponent<TextMeshProUGUI>();
        }

        void Update()
        {           
            float fpsCount = 1f / Time.unscaledDeltaTime;
            accumulatedFPS += fpsCount;
            frameCount++; 
            timeElapsed += Time.unscaledDeltaTime; 

            if(timeElapsed > updateInterval)
            {
                float avgFPS = accumulatedFPS / frameCount;
                fps = Mathf.RoundToInt(avgFPS);
                SetText(fps);
                timeElapsed = 0f;
                accumulatedFPS = 0f;
                frameCount = 0;
            }
        }

        private void SetText(int count)
        {
            if(cachedStrings.ContainsKey(count))
                fpsText.text = cachedStrings[count];
            else
            {
                string c = count.ToString();
                cachedStrings.Add(count, c);
                fpsText.text = c;
            }
        }
    }
}
