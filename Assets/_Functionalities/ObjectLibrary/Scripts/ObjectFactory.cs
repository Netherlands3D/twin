using System;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    public static class ObjectFactory
    {
        public static void Create(Vector3 opticalSpawnPoint, GameObject prefab)
        {
            Place(
                opticalSpawnPoint, 
                spawnPoint => GameObjectCreationAdapter(prefab, spawnPoint)
            );
        }

        public static void Create(Vector3 opticalSpawnPoint, AssetReferenceGameObject reference)
        {
            Place(
                opticalSpawnPoint, 
                spawnPoint => AddressableObjectCreator(spawnPoint, reference)
            );
        }

        private static void Place(Vector3 opticalSpawnPoint, Action<Vector3> creationAdapter)
        {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();
            if (opticalSpawnPoint != Vector3.zero)
            {
                spawnPoint = opticalSpawnPoint;
            }

            creationAdapter(spawnPoint);
        }

        private static void GameObjectCreationAdapter(GameObject prefab, Vector3 spawnPoint)
        {
            EnsureItIsALayerGameObjectWithName(Object.Instantiate(prefab, spawnPoint, prefab.transform.rotation));
        }

        private static void AddressableObjectCreator(Vector3 spawnPoint, AssetReferenceGameObject reference)
        {
            // Now load the Addressable asset
            reference.InstantiateAsync(spawnPoint, Quaternion.identity).Completed += OnAsyncInstantiationComplete;
        }
        
        private static void ReloadShaders(GameObject asset)
        {
            MeshRenderer[] renderers = asset.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                if (!IsPrefab(asset) && renderer.material)
                {
                    renderer.material.shader = Shader.Find(renderer.material.shader.name);
                    continue;
                }

                renderer.sharedMaterial.shader = Shader.Find(renderer.sharedMaterial.shader.name);
            }
        }

        private static bool IsPrefab(GameObject gameObject)
        {
            // A prefab will have a `transform` but no parent if it's a root prefab in play mode
            return string.IsNullOrEmpty(gameObject.scene.name);
        }

        private static void OnAsyncInstantiationComplete(AsyncOperationHandle<GameObject> handle)
        {
            if (!handle.IsValid() || handle.Result == null)
            {
                Debug.LogWarning($"Object {handle.DebugName} could not be instantiated");
                return;
            }

            EnsureItIsALayerGameObjectWithName(handle.Result);
            ReloadShaders(handle.Result);
        }

        private static void EnsureItIsALayerGameObjectWithName(GameObject newObject)
        {
            var layerComponent = newObject.GetComponent<HierarchicalObjectLayerGameObject>();
            if (!layerComponent)
            {
                layerComponent = newObject.AddComponent<HierarchicalObjectLayerGameObject>();
            }

            layerComponent.Name = newObject.name;
        }
    }
}