using UnityEngine;

namespace Netherlands3D.Tilekit.Profiling
{
    public sealed class TelemetryPublisher : MonoBehaviour
    {
        private void LateUpdate()
        {
            Telemetry.EndOfFramePublish();
        }

        private void OnDestroy()
        {
            Telemetry.Shutdown();
        }
    }
}