using System;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class GameOverUI : MonoBehaviour
    {
        public GameManager gameManager;
        public TextMeshProUGUI distanceText;
        public TextMeshProUGUI timeText;
        
        private void OnEnable()
        {
            distanceText.text = gameManager.RoundedDistance + " km";
            timeText.text = gameManager.TimeSinceStart.ToString("m\\:ss");
        }
    }
}