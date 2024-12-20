using System;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class Timer : MonoBehaviour
    {
        public float PenaltyTimePerBooth = 5;
        public bool TimeIsRunning = false; 
        private float initialTime;

        public UnityEvent<float> Tick;
        public UnityEvent<string> TickAsTimeString;
        public UnityEvent<float> Finished;
        private float finishedTime;

        public float FinishedTime
        {
            get => finishedTime;
            set => finishedTime = value;
        }

        public void Reset()
        {
            TickAsTimeString.Invoke("00:00");
            Tick.Invoke(0f);
        }

        public void StartTimer()
        {
            TimeIsRunning = true;
            initialTime = Time.timeSinceLevelLoad;
        }

        private void Update()
        {
            if (!TimeIsRunning) return;

            var seconds = Time.timeSinceLevelLoad - initialTime;
            Tick.Invoke(seconds);
            
            TimeSpan time = TimeSpan.FromSeconds(seconds);

            string str = time.ToString(@"mm\:ss");
            TickAsTimeString.Invoke(str);
        }

        public void Finish()
        {
            TimeIsRunning = false;
            FinishedTime = Time.timeSinceLevelLoad - initialTime;

            float penaltyTime = 0;
            StempelTrigger[] triggers = FindObjectsOfType<StempelTrigger>();
            foreach (StempelTrigger trigger in triggers)
                if (!trigger.IsCollected)
                    penaltyTime += PenaltyTimePerBooth;
            FinishedTime += penaltyTime;

            Finished.Invoke(FinishedTime);
        }
    }
}
