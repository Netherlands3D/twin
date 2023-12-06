using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ToolButton : MonoBehaviour
    {
        [SerializeField] private Tool tool;   
        [SerializeField] private Toolbar toolbar;

        [Header("Tool button visuals")]
        [SerializeField] private GameObject enabledObjects;
        [SerializeField] private GameObject disabledObjects;

        public Tool Tool { get => tool; private set => tool = value; }

        private void OnValidate() {
            if(toolbar) return;

            toolbar = GetComponentInParent<Toolbar>();
        }

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

        public void ToggleWithoutNotify(bool active, bool destroySpawnedPrefabs = false)
        {
            tool.Open = active;
            enabledObjects.SetActive(active);
            disabledObjects.SetActive(!active);

            if(destroySpawnedPrefabs)
            {
                tool.DestroyPrefabInstances();
            }
        }
    }
}
