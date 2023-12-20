using System;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class GameManager : MonoBehaviour
    {
        public enum GameState
        {
            StartScreen,
            Playing,
            GameOverScreen
        }
        
        public float distanceInKilometers = 0f;
        public float gameStartedAt = 0f;
        public GameState gameState = GameState.StartScreen;
        
        [SerializeField] private UnityEvent onGameStarted;
        [SerializeField] private UnityEvent onGameFinished;
        [SerializeField] private UnityEvent onGameRestarted;

        public TimeSpan TimeSinceStart => new (0, 0, (int)(Time.realtimeSinceStartup - gameStartedAt));
        public int RoundedDistance => (int)distanceInKilometers;

        private void Start()
        {
            StartGame();
        }

        public void StartGame()
        {
            gameStartedAt = Time.realtimeSinceStartup;
            distanceInKilometers = 0;
            gameState = GameState.Playing;
            onGameStarted.Invoke();
        }

        public void UpdateDistance(float distanceInKilometers)
        {
            this.distanceInKilometers = distanceInKilometers;
        }

        public void Finished()
        {
            gameState = GameState.GameOverScreen;
            onGameFinished.Invoke();
        }

        public void RestartGame()
        {
            gameState = GameState.StartScreen;
            onGameRestarted.Invoke();
        }
    }
}