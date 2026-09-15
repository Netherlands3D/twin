using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.Twin.Services
{
    // Should be be called by Functionality_DebugInfo; when Functionality_DebugInfo is enabled,
    // also enable Tool_DebugStats, except in release builds.
    public class DebugStatsToolAvailabilityChanger : MonoBehaviour
    {
        private int availableCounter;

        public void ChangeDebugStatsToolAvailability(bool available)
        {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            if (!HasDebugStatsUrlParameter())
                return;
#endif
            
            var debugStatsTool = ServiceLocator.GetService<ToolService>().GetTool(ToolType.DebugStats);
            debugStatsTool.SetAvailability(available);
        }
        
        
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        private static bool HasDebugStatsUrlParameter()
        {
            if (!Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out var url))
                return false;

            return QueryString.Decode(url.Query).ContainsKey("debugstats");
        }
#endif
        
    }
}
