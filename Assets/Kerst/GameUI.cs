using System;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class GameUI : MonoBehaviour
    {
        public GameManager gameManager;
        public TextMeshProUGUI distanceText;
        public TextMeshProUGUI timeText;
        
        private void Update()
        {
            distanceText.text = gameManager.RoundedDistance + " km";
            timeText.text = gameManager.TimeSinceStart.ToString("m\\:ss");
        }
    }
}