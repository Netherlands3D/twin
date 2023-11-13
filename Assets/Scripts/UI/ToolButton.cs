using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ToolButton : MonoBehaviour
    {
        [SerializeField] private Tool tool;   
        [SerializeField] private Toolbar toolbar;

        [SerializeField] private GameObject enabledObjects;
        [SerializeField] private GameObject disabledObjects;

        public Tool Tool { get => tool; private set => tool = value; }

        public void Toggle()
        {
            Tool.ToggleInspector();

            enabledObjects.SetActive(Tool.Open);
            disabledObjects.SetActive(!Tool.Open);

            if(toolbar)
            {
                toolbar.DisableOutsideToolGroup(Tool);
            }
        }

        public void ToggleWithoutNotify(bool active)
        {
            enabledObjects.SetActive(active);
            disabledObjects.SetActive(!active);
        }
    }
}
