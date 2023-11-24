using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class Toolbar : MonoBehaviour
    {
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
                        toolButton.ToggleWithoutNotify(false);
                    }
                }
            }
        }
    }
}
