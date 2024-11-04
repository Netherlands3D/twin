using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Newtonsoft.Json;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Twin
{
    [Serializable]
    public class PrefabGroup
    {
        public string groupName;
        public bool autoPopulateUI;
        public List<LayerGameObject> prefabs;
    }

    //this should not be serialized
    public class PrefabGroupRuntime
    {
        public string groupName;
        public bool autoPopulateUI;
        public List<LayerGameObject> prefabs;
    }

    [CreateAssetMenu(menuName = "Netherlands3D/Twin/PrefabLibrary", fileName = "PrefabLibrary", order = 0)]
    public class PrefabLibrary : ScriptableObject
    {
        [JsonIgnore] public LayerGameObject fallbackPrefab;
        [JsonIgnore] public List<PrefabGroup> prefabGroups;
        [JsonIgnore] public List<PrefabGroupRuntime> prefabGroupsRuntime = new();

        public LayerGameObject GetPrefabById(string id)
        {
            foreach (var group in prefabGroups)
            {
                foreach (var prefab in group.prefabs)
                {
                    if (prefab.PrefabIdentifier == id)
                    {
                        return prefab;
                    }
                }
            }

            return fallbackPrefab;
        }

        public void AddPrefabGroupRuntime(string groupName)
        {
            PrefabGroupRuntime prefabGroupRuntime = new PrefabGroupRuntime();
            prefabGroupRuntime.groupName = groupName;
            prefabGroupRuntime.autoPopulateUI = true;
            prefabGroupRuntime.prefabs = new List<LayerGameObject>();
            prefabGroupsRuntime.Add(prefabGroupRuntime);
        }

        public void AddObjectToPrefabGroupRuntime(string groupName, LayerGameObject layerObject)
        {
            foreach (var group in prefabGroupsRuntime)
            {
                if (group.groupName != groupName) continue;
                
                foreach (LayerGameObject go in group.prefabs)
                {
                    if (go.name != layerObject.name) continue;

                    group.prefabs.Remove(go);
                }

                group.prefabs.Add(layerObject);
            }
        }
    }
}