using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin.Layers
{
    public class LayerSpawner
    {
        private readonly PrefabLibrary prefabLibrary;

        public LayerSpawner(PrefabLibrary prefabLibrary)
        {
            this.prefabLibrary = prefabLibrary;
        }

        public async Task<LayerGameObject> Spawn(ReferencedLayerData layerData)
        {
            var prefab = prefabLibrary.GetPrefabById(layerData.TypeOfLayer);

            var layerGameObject = await Spawn(prefab);
            layerData.ReplaceReference(layerGameObject);

            return layerGameObject;
        }

        public async Task<LayerGameObject> Spawn(
            ReferencedLayerData layerData, 
            Vector3 position, 
            Quaternion rotation
        ) {
            var prefab = prefabLibrary.GetPrefabById(layerData.TypeOfLayer);

            var layerGameObject = await SpawnObject(prefab, position, rotation);
            layerData.ReplaceReference(layerGameObject);

            return layerGameObject;        
        }

        private async Task<LayerGameObject> Spawn(LayerGameObject prefab) 
        {
            return prefab.SpawnLocation switch
            {
                SpawnLocation.OpticalCenter => await SpawnAtOpticalPosition(prefab),
                SpawnLocation.CameraPosition => await SpawnAtCameraPosition(prefab),
                SpawnLocation.PrefabPosition => await SpawnObject(prefab, prefab.transform.position, true),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private async Task<LayerGameObject> SpawnAtOpticalPosition(LayerGameObject prefab)
        {
            var opticalRaycaster = Object.FindAnyObjectByType<OpticalRaycaster>();
            if (!opticalRaycaster)
            {
                // if there is no optical raycaster - we fallback to the ObjectPlacementUtility's SpawnPoint
                return await SpawnObjectAtSpawnPoint(prefab);
            }

            var centerOfViewport = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);

            // Wrap the callback in a Task so that we can stay async
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

        private async Task<LayerGameObject> SpawnAtCameraPosition(LayerGameObject prefab)
        {
            var mainCameraTransform = Camera.main?.transform;

            return await SpawnObject(
                prefab,
                mainCameraTransform?.position ?? Vector3.zero,
                mainCameraTransform?.rotation ?? Quaternion.identity
            );
        }

        private async Task<LayerGameObject> SpawnObject(LayerGameObject prefab, Vector3 position, bool hasHit)
        {
            if (!hasHit)
            {
                // if there is no hit from the optical raycaster - we fallback to the ObjectPlacementUtility's
                // SpawnPoint
                return await SpawnObjectAtSpawnPoint(prefab);
            }

            return await SpawnObject(prefab, position, prefab.transform.rotation);
        }

        private async Task<LayerGameObject> SpawnObject(LayerGameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (position == Vector3.zero)
            {
                return await SpawnObjectAtSpawnPoint(prefab);
            }

            var layerGameObjects = await Object.InstantiateAsync(prefab, position, rotation);
            
            return layerGameObjects.FirstOrDefault();
        }

        private async Task<LayerGameObject> SpawnObjectAtSpawnPoint(LayerGameObject prefab)
        {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();
            
            var layerGameObjects = await Object.InstantiateAsync(prefab, spawnPoint, Quaternion.identity);
            
            return layerGameObjects.FirstOrDefault();
        }
    }
}