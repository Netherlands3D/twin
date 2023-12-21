using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        public Sprite solidPresent;

        public void OnGameStarted(int index)
        {
            presents.transform.ClearAllChildren();
            // Remember: target count is actually the number of targets -1 because the starting position is a target
            for (int i = 0; i < distanceCalculator.targetcount; i++)
            {
                GameObject.Instantiate(presentPrefab, presents.transform);
            }
        }

        public void OnTargetReached(int index)
        {
            // -1 is because the first target is the starting position and is never reached
            presents.transform.GetChild(index - 1).GetComponent<Image>().sprite = solidPresent;
        }

        private void Update()
        {
            distanceText.text = gameManager.RoundedDistance + " km";
            timeText.text = gameManager.TimeSinceStart.ToString("m\\:ss");
        }
    }
}