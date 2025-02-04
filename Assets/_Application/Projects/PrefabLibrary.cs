using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Twin.Projects
{
    [Serializable]
    public class PrefabGroup
    {
        public string groupName;
        public bool autoPopulateUI;
        public List<LayerGameObject> prefabs;
        public List<AssetReferenceGameObject> prefabReferences;
    }

    [CreateAssetMenu(menuName = "Netherlands3D/Twin/PrefabLibrary", fileName = "PrefabLibrary", order = 0)]
    public class PrefabLibrary : ScriptableObject
    {
        public LayerGameObject fallbackPrefab;
        public List<PrefabGroup> prefabGroups;
        [NonSerialized] private List<PrefabGroup> prefabRuntimeGroups = new();
        public List<PrefabGroup> PrefabRuntimeGroups => prefabRuntimeGroups;

        public LayerGameObject GetPrefabById(string id)
        {
            var prefabById = FindPrefabInGroups(id, prefabGroups);
            if (prefabById) return prefabById;
            
            prefabById = FindPrefabInGroups(id, prefabRuntimeGroups);
            if (prefabById) return prefabById;

            return fallbackPrefab;
        }

        public void AddPrefabRuntimeGroup(string groupName)
        {
            prefabRuntimeGroups.Add(
                new PrefabGroup
                {
                    groupName = groupName,
                    autoPopulateUI = true,
                    prefabs = new List<LayerGameObject>()
                }
            );
        }

        public void AddObjectToPrefabRuntimeGroup(string groupName, LayerGameObject layerObject)
        {
            foreach (var group in prefabRuntimeGroups.Where(group => group.groupName == groupName))
            {
                group.prefabs.RemoveAll(go => go.name == layerObject.name);
                group.prefabs.Add(layerObject);
            }
        }

        private LayerGameObject FindPrefabInGroups(string id, List<PrefabGroup> prefabGroups)
        {
            foreach (var group in prefabGroups)
            {
                var findPrefabInGroups = FindPrefabInGroup(id, group);
                if (findPrefabInGroups == null) continue;

                return findPrefabInGroups;
            }

            return null;
        }

        private LayerGameObject FindPrefabInGroup(string id, PrefabGroup group)
        {
            return group.prefabs.FirstOrDefault(prefab => prefab.PrefabIdentifier == id);
        }
    }
}