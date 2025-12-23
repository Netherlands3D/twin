using System.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    [RequireComponent(typeof(Timer))]
    [RequireComponent(typeof(DataSet))]
    public sealed class DataService : MonoBehaviour
    {
        [field:SerializeField] public Timer Timer { get; set; }
        [field:SerializeField] public DataSet DataSet { get; private set; }

        private void Awake()
        {
            Timer ??= GetComponent<Timer>();
            DataSet ??= GetComponent<DataSet>();
        }

        private IEnumerator Start()
        {
            // Wait two frames for the switch to main scene to happen before we do anything
            yield return null;
            yield return null;

            DataSet.Initialize();
            Resume();
        }
        
        private void OnEnable()
        {
            Timer.tick.AddListener(OnTick);
            Resume();
        }

        private void OnDisable()
        {
            Pause();
            Timer.tick.RemoveListener(OnTick);
        }

        private void Pause() => Timer.Pause();

        private void Resume()
        {
            // Timer is only allowed to be resumed if the dataset is initialised.
            if (DataSet.IsInitialized) Timer.Resume();
        }

        private void OnTick() => DataSet.TickedUpdate();

        private void OnDestroy() => DataSet.Dispose();
    }
}