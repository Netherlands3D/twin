using System;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using RSG;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin.Layers
{
    public static class LayerGameObjectFactory
    {
        public static string GetLabel(GameObject prefab)
        {
            return prefab.name;
        }

        /// <summary>
        /// Temporary method of obtaining the label to display in the button - this will use the name in the Addressable
        /// Group to populate the label. In a followup change, we want to load the list of layers from a PluginManifest
        /// ScriptableObject and we can replace the contents of this method.
        /// </summary>
        /// <param name="assetReference"></param>
        /// <returns>Name of the Addressable asset in the AddressableGroup</returns>
        public static string GetLabel(PrefabReference assetReference)
        {
            return assetReference.label;
        }

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
        public static void Create(Vector3 spawnPoint, PrefabReference reference)
        {
            Place(
                spawnPoint, 
                raycastedSpawnPoint => AddressableObjectCreator(raycastedSpawnPoint, reference)
            );
        }

        /// <summary>
        /// Instantiates and places a spatial object from a layer game object prefab at the location provided in the
        /// layer game object.
        /// </summary>
        public static IPromise<LayerGameObject> Create(LayerGameObject layerGameObjectPrefab)
        {
            var layerGameObject = GameObject.Instantiate(layerGameObjectPrefab);
            RefreshShaders(layerGameObject.gameObject);
            EnsureItIsALayerGameObjectWithName(layerGameObject.gameObject);
            
            return Promise<LayerGameObject>.Resolved(layerGameObject);
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

        private static void AddressableObjectCreator(Vector3 spawnPoint, PrefabReference reference)
        {
            reference.referenceGameObject
                .InstantiateAsync(spawnPoint, Quaternion.identity)
                .Completed += handle => OnAsyncInstantiationComplete(reference, handle);
        }
        
        /// <summary>
        /// When loading assets with Renderers, it can happen that the shader is not loaded or loaded too late. This
        /// hack will re-apply the shaders on the given asset so that it will refresh and we know for sure a shader is
        /// applied.
        ///
        /// Without this, you can see pink textures or even no meshes at all in a build.
        /// </summary>
        public static void RefreshShaders(GameObject asset)
        {
            Renderer[] renderers = asset.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (!IsPrefab(asset) && renderer.material)
                {
                    Material[] materials = renderer.materials;
                    foreach (var material in materials)
                    {
                        material.shader = Shader.Find(material.shader.name);
                    }
                    renderer.materials = materials;
                    continue;
                }

                Material[] sharedMaterials = renderer.sharedMaterials;
                foreach (var material in sharedMaterials)
                {
                    material.shader = Shader.Find(material.shader.name);
                }
                renderer.sharedMaterials = sharedMaterials;
            }
        }

        private static bool IsPrefab(GameObject gameObject)
        {
            // A prefab will have a `transform` but no parent if it's a root prefab in play mode
            return string.IsNullOrEmpty(gameObject.scene.name);
        }

        private static void OnAsyncInstantiationComplete(PrefabReference reference, AsyncOperationHandle<GameObject> handle)
        {
            if (!handle.IsValid() || handle.Result == null)
            {
                Debug.LogWarning($"Object {handle.DebugName} could not be instantiated");
                return;
            }

            handle.Result.name = GetLabel(reference);
            EnsureItIsALayerGameObjectWithName(handle.Result);
            RefreshShaders(handle.Result);
        }

        private static void EnsureItIsALayerGameObjectWithName(GameObject newObject)
        {
            var layerComponent = newObject.GetComponent<LayerGameObject>();
            if (!layerComponent)
            {
                // We assume it must be a hierarchical object if there is no LayerGameObject attached
                layerComponent = newObject.AddComponent<HierarchicalObjectLayerGameObject>();
            }

            EnsureItIsALayerGameObjectWithName(layerComponent);
        }

        private static void EnsureItIsALayerGameObjectWithName(LayerGameObject newObject)
        {
            // Ensure the name is a readable one - without this we would get (Clone) as part of the layer names
            newObject.gameObject.name = GetLabel(newObject.gameObject);
            newObject.Name = newObject.gameObject.name;
        }
    }
}