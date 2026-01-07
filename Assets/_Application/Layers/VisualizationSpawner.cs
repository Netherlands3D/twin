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
        public Task<LayerGameObject> Spawn(LayerData layerData)
        {
            if (layerData.PrefabIdentifier == "folder" || string.IsNullOrEmpty(layerData.PrefabIdentifier))
            {
                return null; //a folder has no visualization, if there is no prefab ID, there is no visualization (possibly legacy Folder data structure). The string "folder" comes from the LayerBuilder.Type, and maybe should be changed so we don't have to do a hard check here
            }

            var prefab = prefabLibrary.GetPrefabById(layerData.PrefabIdentifier);
            return SpawnUsingPrefab(layerData, prefab);
        }

        /// <summary>
        /// Spawn a visualisation for the given LayerData at a specific location.
        /// </summary>
        public Task<LayerGameObject> Spawn(
            LayerData layerData,
            Vector3 position,
            Quaternion rotation
        )
        {
            var prefab = prefabLibrary.GetPrefabById(layerData.PrefabIdentifier);
            return SpawnUsingPrefab(layerData, prefab);
        }

        private Task<LayerGameObject> SpawnUsingPrefab(LayerData layerData, LayerGameObject prefab)
        {
            var property = layerData.GetProperty<TransformLayerPropertyData>();
            if (property != null)
            {
                return SpawnObjectAt(
                    prefab,
                    property.UnityPosition,
                    property.Rotation
                );
            }

            return prefab.SpawnLocation switch
            {
                SpawnLocation.Auto => SpawnObject(prefab),
                SpawnLocation.OpticalCenter => SpawnAtOpticalPosition(prefab),
                SpawnLocation.CameraPosition => SpawnAtCameraPosition(prefab),
                SpawnLocation.PrefabPosition => SpawnObjectAt(prefab, prefab.transform.position, prefab.transform.rotation),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private Task<LayerGameObject> SpawnAtOpticalPosition(
            LayerGameObject prefab
        ) {
            var opticalRaycaster = Object.FindAnyObjectByType<OpticalRaycaster>();
            if (!opticalRaycaster)
            {
                // if there is no optical raycaster - we fallback to the ObjectPlacementUtility's SpawnPoint
                return SpawnObjectAtSpawnPoint(prefab);
            }

            var centerOfViewport = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0); //todo: replace with utility function

            // Wrap the callback in a Task so that we can stay async
            var tcs = new TaskCompletionSource<LayerGameObject>();

            //todo: the optical Raycaster uses callbacks instead of Tasks and therefore we have to await both the callback and the spawning
            opticalRaycaster.GetWorldPointAsync(centerOfViewport, async (position, isHit) =>
            {
                try
                {
                    LayerGameObject result;
                    if (isHit)
                    {
                        result = await SpawnObjectAt(prefab, position, prefab.transform.rotation);
                    }
                    else
                    {
                        result = await SpawnObjectAtSpawnPoint(prefab);
                    }
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        private Task<LayerGameObject> SpawnAtCameraPosition(LayerGameObject prefab)
        {
            var mainCameraTransform = Camera.main?.transform;

            return SpawnObjectAt(
                prefab,
                mainCameraTransform?.position ?? Vector3.zero,
                mainCameraTransform?.rotation ?? Quaternion.identity
            );
        }

        private Task<LayerGameObject> SpawnObject(
            LayerGameObject prefab
        )
        {
            return SpawnObjectAt(prefab, prefab.transform.position, prefab.transform.rotation);
        }

        private Task<LayerGameObject> SpawnObjectAtSpawnPoint(
            LayerGameObject prefab
        )
        {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();

            return SpawnObjectAt(prefab, spawnPoint, Quaternion.identity);
        }

        private static Task<LayerGameObject> SpawnObjectAt(
            LayerGameObject prefab,
            Vector3 position,
            Quaternion rotation
        )
        {

            var layerGameObjects = Object.InstantiateAsync(
                prefab,
                position,
                rotation
            );

            return ToSingleTask(layerGameObjects);
        }

        //todo: maybe we can return AsyncInstantiateOperation instead of Task, so we don't need to do this custom wrapping
        private static Task<T> ToSingleTask<T>(AsyncInstantiateOperation<T> op) where T : Object
        {
            var tcs = new TaskCompletionSource<T>();

            if (op.isDone)
            {
                tcs.SetResult(op.Result[0]);
            }
            else
            {
                op.completed += OnCompleted;
            }
            
            void OnCompleted(AsyncOperation _)
            {
                tcs.SetResult(op.Result[0]);
                op.completed -= OnCompleted;
            }

            return tcs.Task;
        }
    }
}