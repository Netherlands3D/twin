using System;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin.Layers
{
    public class VisualizationSpawner : ILayerSpawner
    {
        private readonly PrefabLibrary prefabLibrary;

        public VisualizationSpawner(PrefabLibrary prefabLibrary)
        {
            this.prefabLibrary = prefabLibrary;
        }

        /// <summary>
        /// Spawn a visualisation for the given LayerData.
        /// </summary>
        public async Task<LayerGameObject> Spawn(LayerData layerData)
        {
            if (layerData.PrefabIdentifier == "folder" || string.IsNullOrEmpty(layerData.PrefabIdentifier))
            {
                return null; //a folder has no visualization, if there is no prefab ID, there is no visualization (possibly legacy Folder data structure). The string "folder" comes from the LayerBuilder.Type, and maybe should be changed so we don't have to do a hard check here
            }
            var prefab = prefabLibrary.GetPrefabById(layerData.PrefabIdentifier);
            return await SpawnUsingPrefab(layerData, prefab);
        }

        /// <summary>
        /// Spawn a visualisation for the given LayerData at a specific location.
        /// </summary>
        public async Task<LayerGameObject> Spawn(
            LayerData layerData,
            Vector3 position,
            Quaternion rotation
        ) {
            var prefab = prefabLibrary.GetPrefabById(layerData.PrefabIdentifier);
            return await SpawnUsingPrefab(layerData, prefab);
        }

        /// <summary>
        /// Spawn a visualisation for the given LayerData but use a specific visualisation based on a given prefab identifier.
        /// </summary>
        // public async Task<LayerGameObject> Spawn(
        //     // ReferencedLayerData layerData, 
        //     string prefabId
        // ) {
        //     var prefab = prefabLibrary.GetPrefabById(prefabId);
        //     return await SpawnObject(prefab);
        // }

        private async Task<LayerGameObject> SpawnUsingPrefab(LayerData layerData, LayerGameObject prefab)
        {
            var property = layerData.GetProperty<TransformLayerPropertyData>();
            if (property != null)
            {
                return await SpawnObjectAt(
                    // layerData, 
                    prefab, 
                    property.UnityPosition, 
                    property.Rotation
                );
            }
            
            return prefab.SpawnLocation switch
            {
                SpawnLocation.Auto => await SpawnObject(prefab),
                SpawnLocation.OpticalCenter => await SpawnAtOpticalPosition(prefab),
                SpawnLocation.CameraPosition => await SpawnAtCameraPosition(prefab),
                SpawnLocation.PrefabPosition => await SpawnObjectAt(prefab, prefab.transform.position, prefab.transform.rotation),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private async Task<LayerGameObject> SpawnAtOpticalPosition(
            // ReferencedLayerData layerData,
            LayerGameObject prefab
        ) {
            var opticalRaycaster = Object.FindAnyObjectByType<OpticalRaycaster>();
            if (!opticalRaycaster)
            {
                // if there is no optical raycaster - we fallback to the ObjectPlacementUtility's SpawnPoint
                return await SpawnObjectAtSpawnPoint(prefab);
            }

            var centerOfViewport = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0); //todo: replace with utility function

            // Wrap the callback in a Task so that we can stay async
            var tcs = new TaskCompletionSource<LayerGameObject>();

            opticalRaycaster.GetWorldPointAsync(centerOfViewport, async (position, isHit) =>
            {
                try
                {
                    if (isHit)
                    {
                        var result = await SpawnObjectAt(prefab, position, prefab.transform.rotation);
                        tcs.SetResult(result);
                    }
                    else
                    {
                        var result = await SpawnObjectAtSpawnPoint(prefab);
                        tcs.SetResult(result);
                    }
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

            return await SpawnObjectAt(
                // layerData,
                prefab,
                mainCameraTransform?.position ?? Vector3.zero,
                mainCameraTransform?.rotation ?? Quaternion.identity
            );
        }

        // private async Task<LayerGameObject> SpawnObject(
        //     // ReferencedLayerData layerData, 
        //     LayerGameObject prefab,
        //     Vector3 position, 
        //     bool hasHit
        // ) {
        //     if (!hasHit)
        //     {
        //         // if there is no hit from the optical raycaster - we fallback to the ObjectPlacementUtility's
        //         // SpawnPoint
        //         return await SpawnObjectAtSpawnPoint(prefab);
        //     }
        //
        //     return await SpawnObjectAt(prefab, position, prefab.transform.rotation);
        // }

        private async Task<LayerGameObject> SpawnObject(
            // ReferencedLayerData layerData,
            LayerGameObject prefab
        ) {           
            return await SpawnObjectAt(prefab, prefab.transform.position, prefab.transform.rotation);
        }

        // private async Task<LayerGameObject> SpawnObject(
        //     ReferencedLayerData layerData,
        //     LayerGameObject prefab,
        //     Vector3 position,
        //     Quaternion rotation
        // ) {
        //     if (position == Vector3.zero)
        //     {
        //         return await SpawnObjectAtSpawnPoint(layerData, prefab);
        //     }
        //
        //     return await SpawnObjectAt(layerData, prefab, position, rotation);
        // }

        private async Task<LayerGameObject> SpawnObjectAtSpawnPoint(
            // ReferencedLayerData layerData,
            LayerGameObject prefab
        ) {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();

            return await SpawnObjectAt(prefab, spawnPoint, Quaternion.identity);
        }

        private static async Task<LayerGameObject> SpawnObjectAt(
            // ReferencedLayerData layerData,
            LayerGameObject prefab,
            Vector3 position,
            Quaternion rotation
        ) {
            // var placeholder = layerData.Visualization;
            
            var layerGameObjects = await Object.InstantiateAsync(
                prefab,
                // placeholder.transform,
                position,
                rotation
            );

            return layerGameObjects.FirstOrDefault();
        }
    }
}