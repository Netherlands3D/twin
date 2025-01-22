using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class FPSCounter : MonoBehaviour
    {
        private int fps = 0;
        private TextMeshProUGUI fpsText;

        //since fps wont be a crazy number easily we can cache these strings to prevent any allocation impact
        private Dictionary<int, string> cachedStrings = new Dictionary<int, string>();

        private void Start()
        {
            fpsText = GetComponent<TextMeshProUGUI>();
        }

        // Update is called once per frame
        void Update()
        {
            float fpsCount = 1f / Time.deltaTime;
            fps = Mathf.RoundToInt(fpsCount);
            SetText(fps);            
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
