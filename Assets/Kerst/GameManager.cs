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
        
        public UnityEvent<int> onGameStarted;
        public UnityEvent onGameFinished;
        public UnityEvent onGameRestarted;

        public TimeSpan TimeSinceStart => new (0, 0, (int)(Time.realtimeSinceStartup - gameStartedAt));
        public int RoundedDistance => (int)distanceInKilometers;

        private void Start()
        {
            
        }

        public void StartGame(int scenarioIndex)
        {
            gameStartedAt = Time.realtimeSinceStartup;
            distanceInKilometers = 0;
            gameState = GameState.Playing;
            onGameStarted.Invoke(scenarioIndex);
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