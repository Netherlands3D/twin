using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Tilekit
{
    public class Timer : MonoBehaviour
    {
        // Constant used to signal the UpdateInterval that it needs to trigger each frame.
        private const int UpdateEveryFrame = 0;

        [Tooltip("Whether to immediately start the tile system, or wait until 'Resume' is called")]
        [SerializeField] private bool startsPaused = false;
        [Tooltip("Update interval in ms; or 'UpdateEveryFrame' (0) for every frame")]
        [SerializeField] private int updateInterval = 200;

        public UnityEvent tick = new();
        
        public bool StartsPaused
        {
            get => startsPaused;
            set => startsPaused = value;
        }

        public int UpdateInterval
        {
            get => updateInterval;
            set => updateInterval = value;
        }

        public bool IsPaused { get; private set; } = false;
        
        private void Start()
        {
            if (!StartsPaused) Resume();
        }

        public void Pause()
        {
            IsPaused = true;
            CancelInvoke(nameof(OnTick));
        }

        public void Resume()
        {
            IsPaused = false;
            if (UpdateInterval == UpdateEveryFrame) return;

            InvokeRepeating(nameof(OnTick), 0.0f, UpdateInterval * 0.001f);
        }

        private void Update()
        {
            if (UpdateInterval != UpdateEveryFrame || IsPaused) return;
            
            OnTick();
        }


        public void OnTick()
        {
            this.tick.Invoke();
        }
    }
}