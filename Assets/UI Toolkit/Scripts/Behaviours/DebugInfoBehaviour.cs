using System.Collections;
using Netherlands3D.Twin;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    public class DebugInfoBehaviour : MonoBehaviour
    {
        [SerializeField] private float fpsUpdateInterval = 0.25f;
        [SerializeField] private float memoryUpdateInterval = 0.5f;

        private DebugInfo debugInfo;
        private MemoryStats memoryStats;
        private FPSIndicator fpsIndicator;
        private WaitForSeconds memoryUpdateWait;
        
        private int systemMemorySize;
        private float accumulatedFPS;
        private int frameCount;
        private float timeElapsed;
        private const float BytesPerMegabyte = 1024f * 1024f;

        private void Awake()
        {
            debugInfo = App.UIRoot.Root.Q<DebugInfo>();
            memoryStats = debugInfo.MemoryStats;
            fpsIndicator = debugInfo.FPSIndicator;
            memoryUpdateWait = new WaitForSeconds(memoryUpdateInterval);
            systemMemorySize = SystemInfo.systemMemorySize;
        }

        private void OnEnable()
        {
            StartCoroutine(MemoryTick());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void Update()
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;

            accumulatedFPS += currentFPS;
            frameCount++;
            timeElapsed += Time.unscaledDeltaTime;

            if (timeElapsed < fpsUpdateInterval)
                return;

            int averageFPS = Mathf.RoundToInt(accumulatedFPS / frameCount);
            fpsIndicator.FPSValue = averageFPS;

            timeElapsed = 0f;
            accumulatedFPS = 0f;
            frameCount = 0;
        }

        private IEnumerator MemoryTick()
        {
            while (true)
            {
                memoryStats.SystemValue = SystemInfo.systemMemorySize;
                memoryStats.ManagedValue = ConvertBytesToMegabytes(System.GC.GetTotalMemory(false));
                yield return memoryUpdateWait;
            }
        }

        private static float ConvertBytesToMegabytes(long bytes)
        {
            return bytes / BytesPerMegabyte;
        }
        
        
    }
}