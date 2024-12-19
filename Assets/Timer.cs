using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class Timer : MonoBehaviour
    {
        public bool TimeIsRunning = false; 
        private float initialTime;

        public UnityEvent<float> Tick;
        public UnityEvent<float> Finished;
        private float finishedTime;

        public float FinishedTime
        {
            get => finishedTime;
            set => finishedTime = value;
        }

        public void StartTimer()
        {
            TimeIsRunning = true;
            initialTime = Time.timeSinceLevelLoad;
        }

        private void Update()
        {
            if (!TimeIsRunning) return;

            Tick.Invoke(Time.timeSinceLevelLoad - initialTime);
        }

        public void Finish()
        {
            TimeIsRunning = false;
            FinishedTime = Time.timeSinceLevelLoad - initialTime;
            Finished.Invoke(FinishedTime);
        }
    }
}
