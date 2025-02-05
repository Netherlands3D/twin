using System;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    public static class SpatialObjectFactory
    {
        /// <summary>
        /// Instantiates and places a spatial object from a prefab at the given spawn point or if that
        /// point is Vector3.zero, at the optical center of the screen. 
        /// </summary>
        public static void Create(Vector3 spawnPoint, GameObject prefab)
        {
            Place(
                spawnPoint, 
                raycastedSpawnPoint => GameObjectCreationAdapter(prefab, raycastedSpawnPoint)
            );
        }

        /// <summary>
        /// Instantiates and places a spatial object from an addressable reference at the given spawn point or if that
        /// point is Vector3.zero, at the optical center of the screen. 
        /// </summary>
        public static void Create(Vector3 spawnPoint, AssetReferenceGameObject reference)
        {
            Place(
                spawnPoint, 
                raycastedSpawnPoint => AddressableObjectCreator(raycastedSpawnPoint, reference)
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
            reference.InstantiateAsync(spawnPoint, Quaternion.identity).Completed += OnAsyncInstantiationComplete;
        }
        
        /// <summary>
        /// When loading assets with Renderers, it can happen that the shader is not loaded or loaded too late. This
        /// hack will re-apply the shaders on the given asset so that it will refresh and we know for sure a shader is
        /// applied.
        ///
        /// Without this, you can see pink textures or even no meshes at all in a build.
        /// </summary>
        private static void RefreshShaders(GameObject asset)
        {
            Renderer[] renderers = asset.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
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
            RefreshShaders(handle.Result);
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