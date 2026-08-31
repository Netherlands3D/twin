using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Netherlands3D.Coordinates;
using UnityEngine.Events;

#if SUBOBJECT
using Netherlands3D.SubObjects;
#endif

namespace Netherlands3D.CartesianTiles
{
    [AddComponentMenu("Netherlands3D/CartesianTiles/BinaryMeshLayer")]
    public class BinaryMeshLayer : Layer
    {
        public Dictionary<Vector2Int, ObjectMapping> Mappings = new Dictionary<Vector2Int, ObjectMapping>();
        public UnityEvent<ObjectMapping> OnMappingCreated = new();
        public UnityEvent<ObjectMapping> OnMappingRemoved = new();
        public UnityEvent<Tile> OnTileObjectCreated = new();

#if SUBOBJECT
        public bool hasMetaData = false;
        public string removeFromID;
#endif

        public List<Material> DefaultMaterialList = new List<Material>();
        public ShadowCastingMode tileShadowCastingMode = ShadowCastingMode.On;

        public string brotliCompressedExtention = ".br";

        private GameObject container;
        private MeshRenderer meshRenderer;

        [System.Obsolete("This field is obsolete, use the CreateMeshColliders property instead.")]
        public bool createMeshcollider = false;

        public bool CreateMeshColliders
        {
            get => createMeshcollider;
            set => createMeshcollider = value;
        }

        public override void HandleTile(TileChange tileChange, System.Action<TileChange> callback = null)
        {
            TileAction action = tileChange.action;
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);

            switch (action)
            {
                case TileAction.Create:
                    tiles.Add(tileKey, CreateNewTile(tileKey));
                    break;

                case TileAction.Upgrade:
                    tiles[tileKey].unityLOD++;
                    break;

                case TileAction.Downgrade:
                    tiles[tileKey].unityLOD--;
                    break;

                case TileAction.Remove:
                    InteruptRunningProcesses(tileKey);
                    RemoveGameObject(tileKey);
                    tiles.Remove(tileKey);
                    callback?.Invoke(tileChange);
                    return;
            }

            if (action != TileAction.Remove)
            {
                tiles[tileKey].runningCoroutine =
                    StartCoroutine(DownloadBinaryMesh(tileChange, callback));
            }
        }

        private Tile CreateNewTile(Vector2Int tileKey)
        {
            return new Tile
            {
                unityLOD = 0,
                tileKey = tileKey,
                layer = this
            };
        }

        private void RemoveGameObject(Vector2Int tileKey)
        {
            RemoveVisualisation(tileKey);
            RemoveMapping(tileKey);
        }

        private void RemoveVisualisation(Vector2Int tileKey)
        {
            if (!tiles.TryGetValue(tileKey, out var tile))
                return;

            GameObject tileObject = tile.gameObject;

            if (tileObject == null)
                return;

            MeshFilter[] meshFilters = tileObject.GetComponentsInChildren<MeshFilter>();

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh == null)
                    continue;

                Mesh sharedMesh = meshFilter.sharedMesh;

                meshFilter.sharedMesh = null;

                Destroy(sharedMesh);
            }

            Destroy(tileObject);
        }

        private void RemoveMapping(Vector2Int tileKey)
        {
            if (!Mappings.ContainsKey(tileKey))
                return;

            ObjectMapping mapping = Mappings[tileKey];

            Mappings.Remove(tileKey);

            OnMappingRemoved.Invoke(mapping);
        }

        /// <summary>
        /// Returns the correct dataset path for the current platform.
        ///
        /// WebGL:
        ///     file.bin.br
        ///
        /// Windows/Linux/Editor:
        ///     file.bin
        /// </summary>
        private string GetPlatformDatasetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

#if !UNITY_EDITOR && UNITY_WEBGL

            // WebGL uses the Brotli version.
            if (!string.IsNullOrEmpty(brotliCompressedExtention) &&
                !path.EndsWith(
                    brotliCompressedExtention,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                path += brotliCompressedExtention;
            }

#else

            // Standalone Windows/Linux and Editor use the normal file.
            if (!string.IsNullOrEmpty(brotliCompressedExtention) &&
                path.EndsWith(
                    brotliCompressedExtention,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(
                    0,
                    path.Length - brotliCompressedExtention.Length);
            }

#endif

            return path;
        }

        private IEnumerator DownloadBinaryMesh(
            TileChange tileChange,
            System.Action<TileChange> callback = null)
        {
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);

            if (!tiles.TryGetValue(tileKey, out var tile))
            {
                callback?.Invoke(tileChange);
                yield break;
            }

            int index = tile.unityLOD;

            if (index < 0 || index >= Datasets.Count)
            {
                Debug.LogError(
                    $"Invalid LOD index {index} for tile {tileKey}.");

                callback?.Invoke(tileChange);
                yield break;
            }

            /*
             * Start with the configured dataset path.
             */
            string url = Datasets[index].path;

            /*
             * If a URL is configured, use that instead.
             * This preserves the behavior of the original script.
             */
            if (!string.IsNullOrEmpty(Datasets[index].url))
            {
                url = Datasets[index].url;
            }

            /*
             * Select .bin or .bin.br depending on platform.
             *
             * WebGL      -> .bin.br
             * Standalone -> .bin
             * Editor     -> .bin
             */
            url = GetPlatformDatasetPath(url);

            /*
             * Replace X/Y placeholders.
             */
            url = url.ReplaceXY(tileChange.X, tileChange.Y);

            var webRequest = UnityWebRequest.Get(url);

#if !UNITY_EDITOR && UNITY_WEBGL && ADD_BROTLI_ACCEPT_ENCODING_HEADER
            webRequest.SetRequestHeader("Accept-Encoding", "br");
#endif

            tile.runningWebRequest = webRequest;

            yield return webRequest.SendWebRequest();

            /*
             * Tile may have been removed while downloading.
             */
            if (!tiles.ContainsKey(tileKey))
            {
                webRequest.Dispose();
                yield break;
            }

            tile.runningWebRequest = null;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"Failed to download tile {tileKey} from {url}: {webRequest.error}");

                webRequest.Dispose();

                RemoveGameObject(tileKey);

                callback?.Invoke(tileChange);

                yield break;
            }

            byte[] results = webRequest.downloadHandler.data;

            webRequest.Dispose();

            if (results == null || results.Length == 0)
            {
                Debug.LogError(
                    $"Downloaded empty mesh for tile {tileKey} from {url}");

                RemoveGameObject(tileKey);

                callback?.Invoke(tileChange);

                yield break;
            }

            yield return new WaitUntil(() => pauseLoading == false);

            GameObject newGameobject =
                CreateNewGameObject(
                    url,
                    results,
                    tileChange);

            if (!newGameobject)
            {
                callback?.Invoke(tileChange);
                yield break;
            }

            if (tiles.TryGetValue(tileKey, out tile))
            {
                if (tile.gameObject)
                    RemoveGameObject(tileKey);

                tile.gameObject = newGameobject;

                OnTileObjectCreated.Invoke(tile);

#if SUBOBJECT
                if (hasMetaData)
                {
                    yield return StartCoroutine(
                        LoadMetaData(
                            newGameobject,
                            url,
                            tileChange));
                }
#endif
            }
            else
            {
                /*
                 * Tile was destroyed while the mesh was being created.
                 */
                Destroy(newGameobject);
            }

            callback?.Invoke(tileChange);
        }

#if SUBOBJECT

        private IEnumerator LoadMetaData(
            GameObject gameObject,
            string geometryUrl,
            TileChange tileChange)
        {
            if (gameObject == null)
                yield break;

            /*
             * Convert:
             *
             * xxx.bin
             *
             * to:
             *
             * xxx-data.bin
             */
            string metaDataName = geometryUrl;

            /*
             * Remove .br first if present.
             */
            if (!string.IsNullOrEmpty(brotliCompressedExtention) &&
                metaDataName.EndsWith(
                    brotliCompressedExtention,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                metaDataName = metaDataName.Substring(
                    0,
                    metaDataName.Length -
                    brotliCompressedExtention.Length);
            }

            metaDataName = metaDataName.Replace(
                ".bin",
                "-data.bin");

#if !UNITY_EDITOR && UNITY_WEBGL

            /*
             * WebGL metadata also uses Brotli.
             */
            if (!string.IsNullOrEmpty(brotliCompressedExtention) &&
                !metaDataName.EndsWith(
                    brotliCompressedExtention,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                metaDataName += brotliCompressedExtention;
            }

#endif

            var webRequest =
                UnityWebRequest.Get(metaDataName);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"Failed to download metadata from {metaDataName}: " +
                    webRequest.error);

                webRequest.Dispose();

                yield break;
            }

            if (gameObject == null)
            {
                webRequest.Dispose();
                yield break;
            }

            byte[] results =
                webRequest.downloadHandler.data;

            webRequest.Dispose();

            ReadMetaDataFile(
                results,
                gameObject,
                tileChange);
        }

        private void ReadMetaDataFile(
            byte[] results,
            GameObject gameobject,
            TileChange tileChange)
        {
            /*
             * The GameObject could have been destroyed in the meantime.
             */
            if (!gameobject)
                return;

            Vector2Int tileKey =
                new Vector2Int(
                    tileChange.X,
                    tileChange.Y);

            if (!tiles.ContainsKey(tileKey))
                return;

            ObjectMapping objectMapping =
                gameobject.AddComponent<ObjectMapping>();

            objectMapping.items = new();

            using (var stream = new MemoryStream(results))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    var version = reader.ReadInt32();
                    var subObjects = reader.ReadInt32();

                    for (int i = 0; i < subObjects; i++)
                    {
                        var id = reader.ReadString();
                        var firstIndex = reader.ReadInt32();
                        var indicesCount = reader.ReadInt32();
                        var firstVertex = reader.ReadInt32();
                        var vertexCount = reader.ReadInt32();
                        var subMeshID = reader.ReadInt32();

                        if (!string.IsNullOrEmpty(removeFromID))
                        {
                            id = id.Replace(removeFromID, "");
                        }

                        ObjectMappingItem item =
                            new ObjectMappingItem()
                            {
                                objectID = id,
                                firstVertex = firstVertex,
                                verticesLength = vertexCount
                            };

                        objectMapping.items.Add(id, item);
                    }
                }
            }

            Mappings[tileKey] = objectMapping;

            OnMappingCreated.Invoke(objectMapping);
        }

#endif

        public void EnableShadows(bool enabled)
        {
            tileShadowCastingMode =
                enabled
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off;

            MeshRenderer[] existingTiles =
                GetComponentsInChildren<MeshRenderer>();

            foreach (var renderer in existingTiles)
            {
                renderer.shadowCastingMode =
                    tileShadowCastingMode;
            }
        }

        private GameObject CreateNewGameObject(
            string source,
            byte[] binaryMeshData,
            TileChange tileChange)
        {
            container = new GameObject();

            container.name =
                tileChange.X.ToString() +
                "-" +
                tileChange.Y.ToString();

            container.transform.position =
                new Coordinate(
                    CoordinateSystem.RD,
                    tileChange.X + (tileSize / 2),
                    tileChange.Y + (tileSize / 2)
                ).ToUnity();

            container.SetActive(isEnabled);

            Mesh mesh;

            int[] submeshIndices;

            try
            {
                mesh =
                    BinaryMeshConversion.ReadBinaryMesh(
                        binaryMeshData,
                        out submeshIndices);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Failed to read binary mesh for tile " +
                    $"{tileChange.X}-{tileChange.Y}: {exception}");

                Destroy(container);

                return null;
            }

            if (mesh == null)
            {
                Debug.LogError(
                    $"BinaryMeshConversion returned null for tile " +
                    $"{tileChange.X}-{tileChange.Y}");

                Destroy(container);

                return null;
            }

            /*
             * Remove .br from the mesh name.
             */
            if (!string.IsNullOrEmpty(brotliCompressedExtention) &&
                source.EndsWith(
                    brotliCompressedExtention,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                source = source.Substring(
                    0,
                    source.Length -
                    brotliCompressedExtention.Length);
            }

            mesh.name = source;

            MeshFilter meshFilter =
                container.AddComponent<MeshFilter>();

            meshFilter.mesh = mesh;

            /*
             * Set parent after adding MeshFilter.
             *
             * This is important for SubObjects.
             */
            container.transform.parent =
                transform.gameObject.transform;

            container.layer =
                container.transform.parent.gameObject.layer;

            meshRenderer =
                container.AddComponent<MeshRenderer>();

            List<Material> materialList =
                new List<Material>();

            for (int i = 0; i < submeshIndices.Length; i++)
            {
                int materialIndex =
                    submeshIndices[i];

                if (materialIndex >= 0 &&
                    materialIndex < DefaultMaterialList.Count)
                {
                    materialList.Add(
                        DefaultMaterialList[materialIndex]);
                }
                else
                {
                    Debug.LogWarning(
                        $"Invalid material index {materialIndex} " +
                        $"for tile {tileChange.X}-{tileChange.Y}");

                    materialList.Add(null);
                }
            }

            meshRenderer.sharedMaterials =
                materialList.ToArray();

            meshRenderer.shadowCastingMode =
                tileShadowCastingMode;

            if (CreateMeshColliders)
            {
                container.AddComponent<MeshCollider>().sharedMesh =
                    mesh;
            }

            return container;
        }

        /// <summary>
        /// Adds mesh colliders to the meshes found within this layer.
        /// </summary>
        /// <param name="onlyTileUnderPosition">
        /// Optional world position where this tile should be close to.
        /// </param>
        public void AddMeshColliders(
            Vector3 onlyTileUnderPosition = default)
        {
            MeshCollider meshCollider;

            MeshFilter[] meshFilters =
                gameObject.GetComponentsInChildren<MeshFilter>();

            if (meshFilters != null)
            {
                if (onlyTileUnderPosition != default)
                {
                    foreach (MeshFilter meshFilter in meshFilters)
                    {
                        if (
                            Mathf.Abs(
                                onlyTileUnderPosition.x -
                                meshFilter.gameObject.transform.position.x
                            ) < tileSize
                            &&
                            Mathf.Abs(
                                onlyTileUnderPosition.z -
                                meshFilter.gameObject.transform.position.z
                            ) < tileSize
                        )
                        {
                            meshCollider =
                                meshFilter.gameObject
                                    .GetComponent<MeshCollider>();

                            if (meshCollider == null)
                            {
                                meshFilter.gameObject
                                    .AddComponent<MeshCollider>()
                                    .sharedMesh =
                                    meshFilter.sharedMesh;
                            }
                        }
                    }

                    return;
                }

                /*
                 * Add MeshColliders to all meshes if no
                 * specific area was supplied.
                 */
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    meshCollider =
                        meshFilter.gameObject
                            .GetComponent<MeshCollider>();

                    if (meshCollider == null)
                    {
                        meshFilter.gameObject
                            .AddComponent<MeshCollider>()
                            .sharedMesh =
                            meshFilter.sharedMesh;
                    }
                }
            }
        }
    }
}