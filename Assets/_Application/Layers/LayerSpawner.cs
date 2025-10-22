using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin.Layers
{
    public class LayerSpawner : ILayerSpawner
    {
        private readonly PrefabLibrary prefabLibrary;

        public LayerSpawner(PrefabLibrary prefabLibrary)
        {
            this.prefabLibrary = prefabLibrary;
        }

        public async Task<LayerGameObject> Spawn(ReferencedLayerData layerData)
        {
            var prefab = prefabLibrary.GetPrefabById(layerData.PrefabIdentifier);

            return await SpawnUsingLayerGameObject(layerData, prefab);
        }

        public async Task<LayerGameObject> Spawn(string prefabId)
        {
            var prefab = prefabLibrary.GetPrefabById(prefabId);

            return await SpawnObject(prefab);
        }

        public async Task<LayerGameObject> Spawn(
            ReferencedLayerData layerData,
            Vector3 position,
            Quaternion rotation
        ) {
            var prefab = prefabLibrary.GetPrefabById(layerData.PrefabIdentifier);

            return await SpawnObject(layerData, prefab, position, rotation);
        }

        private async Task<LayerGameObject> SpawnUsingLayerGameObject(ReferencedLayerData layerData, LayerGameObject prefab)
        {
            var property = layerData.GetProperty<TransformLayerPropertyData>();
            if (property != null)
            {
                return await SpawnObjectAt(
                    layerData, 
                    prefab, 
                    property.UnityPosition, 
                    property.Rotation
                );
            }
            
            return prefab.SpawnLocation switch
            {
                SpawnLocation.Auto => await SpawnObject(layerData, prefab),
                SpawnLocation.OpticalCenter => await SpawnAtOpticalPosition(layerData, prefab),
                SpawnLocation.CameraPosition => await SpawnAtCameraPosition(layerData, prefab),
                SpawnLocation.PrefabPosition => await SpawnObject(layerData, prefab, prefab.transform.position, true),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private async Task<LayerGameObject> SpawnAtOpticalPosition(
            ReferencedLayerData layerData,
            LayerGameObject prefab
        ) {
            var opticalRaycaster = Object.FindAnyObjectByType<OpticalRaycaster>();
            if (!opticalRaycaster)
            {
                // if there is no optical raycaster - we fallback to the ObjectPlacementUtility's SpawnPoint
                return await SpawnObjectAtSpawnPoint(layerData, prefab);
            }

            var centerOfViewport = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);

            // Wrap the callback in a Task so that we can stay async
            var tcs = new TaskCompletionSource<LayerGameObject>();

            opticalRaycaster.GetWorldPointAsync(centerOfViewport, async (position, isHit) =>
            {
                try
                {
                    var result = await SpawnObject(layerData, prefab, position, isHit);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return await tcs.Task;
        }

        private async Task<LayerGameObject> SpawnAtCameraPosition(ReferencedLayerData layerData, LayerGameObject prefab)
        {
            var mainCameraTransform = Camera.main?.transform;

            return await SpawnObject(
                layerData,
                prefab,
                mainCameraTransform?.position ?? Vector3.zero,
                mainCameraTransform?.rotation ?? Quaternion.identity
            );
        }

        private async Task<LayerGameObject> SpawnObject(
            ReferencedLayerData layerData, 
            LayerGameObject prefab,
            Vector3 position, 
            bool hasHit
        ) {
            if (!hasHit)
            {
                // if there is no hit from the optical raycaster - we fallback to the ObjectPlacementUtility's
                // SpawnPoint
                return await SpawnObjectAtSpawnPoint(layerData, prefab);
            }

            return await SpawnObject(layerData, prefab, position, prefab.transform.rotation);
        }

        private async Task<LayerGameObject> SpawnObject(
            ReferencedLayerData layerData,
            LayerGameObject prefab
        ) {
            var placeholder = layerData.Reference;
            
            var layerGameObjects = await Object.InstantiateAsync(prefab, placeholder.transform);

            return layerGameObjects.FirstOrDefault();
        }

        private async Task<LayerGameObject> SpawnObject(           
            LayerGameObject prefab
        )
        {
            var layerGameObjects = await Object.InstantiateAsync(prefab);
            return layerGameObjects.FirstOrDefault();
        }

        private async Task<LayerGameObject> SpawnObject(
            ReferencedLayerData layerData,
            LayerGameObject prefab,
            Vector3 position,
            Quaternion rotation
        ) {
            if (position == Vector3.zero)
            {
                return await SpawnObjectAtSpawnPoint(layerData, prefab);
            }

            return await SpawnObjectAt(layerData, prefab, position, rotation);
        }

        private async Task<LayerGameObject> SpawnObjectAtSpawnPoint(
            ReferencedLayerData layerData,
            LayerGameObject prefab
        ) {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();

            return await SpawnObjectAt(layerData, prefab, spawnPoint, Quaternion.identity);
        }

        private static async Task<LayerGameObject> SpawnObjectAt(
            ReferencedLayerData layerData,
            LayerGameObject prefab,
            Vector3 position,
            Quaternion rotation
        ) {
            var placeholder = layerData.Reference;
            
            var layerGameObjects = await Object.InstantiateAsync(
                prefab,
                placeholder.transform,
                position,
                rotation
            );

            return layerGameObjects.FirstOrDefault();
        }
    }
}