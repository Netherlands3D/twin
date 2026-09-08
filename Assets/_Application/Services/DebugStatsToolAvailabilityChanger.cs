using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D
{
    // Should be be called by Functionality_DebugInfo; when Functionality_DebugInfo is enabled,
    // also enable Tool_DebugStats, except in release builds.
    public class DebugStatsToolAvailabilityChanger : MonoBehaviour
    {
        private int availableCounter;

        public void ChangeDebugStatsToolAvailability(bool available)
        {
            var debugStatsTool = ServiceLocator.GetService<ToolService>().GetTool(ToolType.DebugStats);
            
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            // Don't show debugStats in a release build for the users.
            // A developer can still turn it on when really persistent.
            if (available)
            {
                available = false;
                availableCounter++;
                if (availableCounter >= 10)
                {
                    available = true;
                    availableCounter = 0;
                }
            }
#endif
            debugStatsTool.SetAvailability(available);
            
        }
        
    }
}
