using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin.Layers
{
    public class LayerSpawner
    {
        public async Task<LayerGameObject> Spawn(
            LayerGameObject prefab, 
            Vector3? position, 
            Quaternion? rotation
        ) {
            var mainCamera = Camera.main;
            
            // position was overridden - so we ignore the default spawn location because the caller dictated it should
            // be at a specific position
            if (position.HasValue)
            {
                return await SpawnObject(
                    prefab,
                    position.Value,
                    rotation ?? Quaternion.identity
                );
            }

            var resultingLayer = prefab.SpawnLocation switch
            {
                SpawnLocation.OpticalCenter => await SpawnAtOpticalPosition(prefab),
                SpawnLocation.CameraPosition => await SpawnObject(
                    prefab, 
                    mainCamera.transform.position,
                    mainCamera.transform.rotation
                ),
                SpawnLocation.PrefabPosition => await SpawnObject(prefab, prefab.transform.position, true),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            Debug.Log($"Spawned layer {resultingLayer}");
            return resultingLayer;
        }

        private async Task<LayerGameObject> SpawnAtOpticalPosition(LayerGameObject prefab)
        {
            var opticalRaycaster = Object.FindAnyObjectByType<OpticalRaycaster>();
            if (!opticalRaycaster)
                return null;

            var centerOfViewport = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);

            // Wrap the callback in a Task
            var tcs = new TaskCompletionSource<LayerGameObject>();

            opticalRaycaster.GetWorldPointAsync(centerOfViewport, async (position, isHit) =>
            {
                try
                {
                    var result = await SpawnObject(prefab, position, isHit);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return await tcs.Task;
        }

        private async Task<LayerGameObject> SpawnObject(LayerGameObject prefab, Vector3 position, bool hasHit)
        {
            if (!hasHit) return null;

            return await SpawnObject(prefab, position, prefab.transform.rotation);
        }

        private async Task<LayerGameObject> SpawnObject(LayerGameObject prefab, Vector3 position, Quaternion rotation)
        {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();
            if (position != Vector3.zero)
            {
                spawnPoint = position;
            }
            
            var layerGameObjects = await Object.InstantiateAsync(prefab, spawnPoint, rotation);
            
            return layerGameObjects.FirstOrDefault();
        }
    }
}