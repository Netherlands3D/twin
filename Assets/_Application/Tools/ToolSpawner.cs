using UnityEngine;

namespace Netherlands3D.Twin.Tools
{
    public class ToolSpawner : MonoBehaviour
    {
        [Header("Group these tools their world prefab instances when they are activated")]
        [Tooltip("The tool data object references to listen to")]
        [SerializeField] private Tool[] tools;

        private void OnEnable() {
            //If the tool is opened in the inspector, spawn the prefab instances
            foreach(var tool in tools){
                tool.onToggleInspector.AddListener(OnToggleInspector);
            }
        }

        private void OnDisable() {
            foreach(var tool in tools){
                tool.onToggleInspector.RemoveListener(OnToggleInspector);
            }
        }

        private void OnToggleInspector(Tool tool)
        {
            if(tool.Open)
            {
                tool.SpawnPrefabInstances(transform);
            }
            else
            {
                tool.DestroyPrefabInstances();
            }
        }
    }
}
