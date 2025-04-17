using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Tilekit
{
    public class Timer : MonoBehaviour
    {
        // Constant used to signal the UpdateInterval that it needs to trigger each frame.
        public const int UpdateEveryFrame = 0;

        [Tooltip("Whether to immediately start the tile system, or wait until 'Resume' is called")]
        [SerializeField] private bool autoStart = true;
        [Tooltip("Update interval in ms; or 'UpdateEveryFrame' (0) for every frame")]
        [SerializeField] private int updateInterval = 200;

        public UnityEvent tick = new();
        
        public bool AutoStart
        {
            get => autoStart;
            set => autoStart = value;
        }

        public int UpdateInterval
        {
            get => updateInterval;
            set => updateInterval = value;
        }

        public bool IsPaused { get; private set; } = false;
        
        private void Start()
        {
            if (AutoStart) Resume();
        }

        public void Pause()
        {
            IsPaused = true;
            CancelInvoke(nameof(tick));
        }

        private void Resume()
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