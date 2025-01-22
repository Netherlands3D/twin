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

        private void Start()
        {
            fpsText = GetComponent<TextMeshProUGUI>();
        }

        // Update is called once per frame
        void Update()
        {
            float fpsCount = 1f / Time.deltaTime;
            fps = Mathf.RoundToInt(fpsCount);
            SetText(fps.ToString());            
        }

        private void SetText(string text)
        {
            fpsText.text = text;
        }


        private Dictionary<int, string> cachedStrings = new Dictionary<int, string>();
    }
}
