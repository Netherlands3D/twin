using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class Toolbar : MonoBehaviour
    {
        /// <summary>
        /// Disable all tools that have a conflicting function group with the given tool, and are activated from this toolbar
        /// </summary>
        /// <param name="tool">The tool to check the others against</param>
        public void DisableOutsideToolGroup(Tool tool)
        {
            foreach (Transform child in transform)
            {
                var toolButton = child.GetComponent<ToolButton>();
                if (toolButton.Tool != tool)
                {
                    var conflictingFunctionGroups = tool.functionGroups.Intersect(toolButton.Tool.functionGroups);
                    if (conflictingFunctionGroups.Any())
                    {
                        toolButton.ToggleWithoutNotify(false,true);
                    }
                }
            }
        }
    }
}
