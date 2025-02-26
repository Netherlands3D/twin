using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using RSG;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Netherlands3D.Twin.Projects
{
    [Serializable]
    public struct PrefabReference
    {
        public string id;
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
        
        // Cached spawner - will become null if scene changes
        private AsyncLoadingScreenSpawner loadingScreenSpawner;

        public List<PrefabGroup> PrefabRuntimeGroups => prefabRuntimeGroups;

        public IPromise<LayerGameObject> GetPrefabById(string id)
        {
            var prefab = FindPrefabInGroups(id, prefabGroups);
            if (prefab) return Promise<LayerGameObject>.Resolved(prefab);

            var prefabReference = FindPrefabReferenceInGroups(id, prefabGroups);
            if (prefabReference.HasValue) return GetPrefabByReference(prefabReference.Value);

            prefab = FindPrefabInGroups(id, prefabRuntimeGroups);
            if (prefab) return Promise<LayerGameObject>.Resolved(prefab);

            prefabReference = FindPrefabReferenceInGroups(id, prefabRuntimeGroups);
            if (prefabReference.HasValue) return GetPrefabByReference(prefabReference.Value);
            
            return Promise<LayerGameObject>.Resolved(fallbackPrefab);
        }

        private IPromise<LayerGameObject> GetPrefabByReference(PrefabReference prefabReference)
        {
            var promise = new Promise<LayerGameObject>();
            
            void OnCompleted(AsyncOperationHandle<GameObject> handle)
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    promise.Resolve(handle.Result.GetComponent<LayerGameObject>());
                    return;
                }

                promise.Reject(handle.OperationException);
            }

            var handle = Addressables.LoadAssetAsync<GameObject>(prefabReference.referenceGameObject);
            handle.Completed += OnCompleted;

            AsyncLoadingScreenSpawner.Instance().Spawn(prefabReference.label + " aan het plaatsen", handle);

            return promise;
        }

        public IPromise<LayerGameObject> Instantiate(string prefabId)
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

        public void AddObjectToPrefabRuntimeGroup(string groupName, string id, string label, AssetReferenceGameObject layerObject)
        {
            var group = prefabGroups.FirstOrDefault(group => group.groupName == groupName) 
                ?? AddPrefabRuntimeGroup(groupName);

            group.prefabReferences.Add(
                new PrefabReference
                {
                    id = id,
                    referenceGameObject = layerObject,
                    label = label
                }
            );
        }

        private PrefabReference? FindPrefabReferenceInGroups(string id, List<PrefabGroup> prefabGroups)
        {
            foreach (var group in prefabGroups)
            {
                var findPrefabInGroups = FindPrefabReferenceInGroup(id, group);
                if (string.IsNullOrEmpty(findPrefabInGroups.id)) continue;

                return findPrefabInGroups;
            }

            return null;
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

        private PrefabReference FindPrefabReferenceInGroup(string id, PrefabGroup group)
        {
            return group.prefabReferences.FirstOrDefault(prefab => prefab.id == id);
        }
    }
}