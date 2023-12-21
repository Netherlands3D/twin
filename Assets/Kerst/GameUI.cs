using System;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class GameUI : MonoBehaviour
    {
        public DistanceCalculator distanceCalculator;
        public GameManager gameManager;
        public TextMeshProUGUI distanceText;
        public TextMeshProUGUI timeText;

        public GameObject presents;
        public GameObject presentPrefab;

        private void Start()
        {
            gameManager.onGameStarted.AddListener(OnGameStarted);
        }

        private void OnGameStarted(int index)
        {
            presents.transform.ClearAllChildren();
            for (int i = 0; i < distanceCalculator.targetcount; i++)
            {
                GameObject.Instantiate(presentPrefab, presents.transform);
            }
        }

        private void Update()
        {
            distanceText.text = gameManager.RoundedDistance + " km";
            timeText.text = gameManager.TimeSinceStart.ToString("m\\:ss");
        }
    }
}