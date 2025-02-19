using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using RSG;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Netherlands3D.Twin.Projects
{
    [Serializable]
    public struct PrefabReference
    {
        public string label;
        public AssetReferenceGameObject referenceGameObject;
    }

    [Serializable]
    public class PrefabGroup
    {
        public string groupName;
        public bool autoPopulateUI;
        public List<LayerGameObject> prefabs = new ();
        public List<PrefabReference> prefabReferences = new ();
    }

    [CreateAssetMenu(menuName = "Netherlands3D/Twin/PrefabLibrary", fileName = "PrefabLibrary", order = 0)]
    public class PrefabLibrary : ScriptableObject
    {
        public LayerGameObject fallbackPrefab;
        public List<PrefabGroup> prefabGroups;
        [NonSerialized] private List<PrefabGroup> prefabRuntimeGroups = new();
        public List<PrefabGroup> PrefabRuntimeGroups => prefabRuntimeGroups;

        public IPromise<LayerGameObject> GetPrefabById(string id)
        {
            var prefab = FindPrefabInGroups(id, prefabGroups);
            if (prefab) return Promise<LayerGameObject>.Resolved(prefab);
            
            prefab = FindPrefabInGroups(id, prefabRuntimeGroups);
            if (prefab) return Promise<LayerGameObject>.Resolved(prefab);

            return Promise<LayerGameObject>.Resolved(fallbackPrefab);
        }
        
        public IPromise<LayerGameObject> Instantiate(string prefabId)
        {
            return ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId)
                .Then(GameObject.Instantiate);
        }

        public PrefabGroup AddPrefabRuntimeGroup(string groupName)
        {
            var prefabGroup = new PrefabGroup
            {
                groupName = groupName,
                autoPopulateUI = true,
                prefabs = new List<LayerGameObject>(),
                prefabReferences = new List<PrefabReference>()
            };
            prefabRuntimeGroups.Add(prefabGroup);

            return prefabGroup;
        }

        public void AddObjectToPrefabRuntimeGroup(string groupName, LayerGameObject layerObject)
        {
            foreach (var group in prefabRuntimeGroups.Where(group => group.groupName == groupName))
            {
                group.prefabs.RemoveAll(go => go.name == layerObject.name);
                group.prefabs.Add(layerObject);
            }
        }

        public void AddObjectToPrefabRuntimeGroup(string groupName, string label, AssetReferenceGameObject layerObject)
        {
            var group = prefabGroups.FirstOrDefault(group => group.groupName == groupName) 
                ?? AddPrefabRuntimeGroup(groupName);

            group.prefabReferences.Add(
                new PrefabReference
                {
                    referenceGameObject = layerObject,
                    label = label
                }
            );
        }

        private LayerGameObject FindPrefabInGroups(string id, List<PrefabGroup> prefabGroups)
        {
            foreach (var group in prefabGroups)
            {
                var findPrefabInGroups = FindPrefabInGroup(id, group);
                if (!findPrefabInGroups) continue;

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