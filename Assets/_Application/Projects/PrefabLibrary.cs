using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using RSG;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    [Serializable]
    public class PrefabGroup
    {
        public string groupName;
        public bool autoPopulateUI;
        public List<LayerGameObject> prefabs = new ();
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

        public IPromise<LayerGameObject> Instantiate(string prefabId, ReferencedLayerData layerData = null)
        {
            IPromise<LayerGameObject> promise = GetPrefabById(prefabId);
            promise.Then(LayerGameObjectFactory.Create);
            promise.Catch(Debug.LogException);
            
            return promise;
        }

        public PrefabGroup AddPrefabRuntimeGroup(string groupName)
        {
            var prefabGroup = new PrefabGroup
            {
                groupName = groupName,
                autoPopulateUI = true,
                prefabs = new List<LayerGameObject>()
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