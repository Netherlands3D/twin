using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ToolSpawner : MonoBehaviour
    {
        [Header("Spawn these tools their world prefabs when they are opened in the inspector")]
        [Tooltip("The tool data object references to listen to")]
        [SerializeField] private Tool[] tools;

        private void Awake() {
            foreach(var tool in tools){
                tool.onToggleInspector.AddListener(OnToggleInspector);
            }
        }

        private void OnToggleInspector(Tool tool)
        {
            if(tool.Open)
            {
                tool.SpawnPrefabInstances();
            }
            else
            {
                tool.DestroyPrefabInstances();
            }
        }
    }
}
