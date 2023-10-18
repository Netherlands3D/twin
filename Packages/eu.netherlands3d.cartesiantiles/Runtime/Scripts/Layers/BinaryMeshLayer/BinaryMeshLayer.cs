

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Netherlands3D.Coordinates;
#if SUBOBJECT
using Netherlands3D.subobject;
#endif
using System.Diagnostics;

namespace Netherlands3D.CartesianTiles
{
    public class BinaryMeshLayer : Layer
    {
#if SUBOBJECT
        public bool hasMetaData = false;
        public string removeFromID;
#endif
        public List<Material> DefaultMaterialList = new List<Material>();
        public bool createMeshcollider = false;
        public ShadowCastingMode tileShadowCastingMode = ShadowCastingMode.On;

        public string brotliCompressedExtention = ".br";
        
        private GameObject container;
        private Mesh mesh;
        private MeshRenderer meshRenderer;

        public override void HandleTile(TileChange tileChange, System.Action<TileChange> callback = null)
        {
            TileAction action = tileChange.action;
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
            switch (action)
            {
                case TileAction.Create:
                    Tile newTile = CreateNewTile(tileKey);
                    tiles.Add(tileKey, newTile);
                    break;
                case TileAction.Upgrade:
                    tiles[tileKey].unityLOD++;
                    break;
                case TileAction.Downgrade:
                    tiles[tileKey].unityLOD--;
                    break;
                case TileAction.Remove:
                    InteruptRunningProcesses(tileKey);
                    RemoveGameObjectFromTile(tileKey);
                    tiles.Remove(tileKey);
                    callback(tileChange);
                    return;
                default:
                    break;
            }
            tiles[tileKey].runningCoroutine = StartCoroutine(DownloadBinaryMesh(tileChange, callback));
        }

        private Tile CreateNewTile(Vector2Int tileKey)
        {
            Tile tile = new Tile();
            tile.unityLOD = 0;
            tile.tileKey = tileKey;
            tile.layer = transform.gameObject.GetComponent<Layer>();


            return tile;
        }
        private void RemoveGameObjectFromTile(Vector2Int tileKey)
        {
            if (tiles.ContainsKey(tileKey))
            {
                Tile tile = tiles[tileKey];
                if (tile == null)
                {
                    return;
                }
                if (tile.gameObject == null)
                {
                    return;
                }
                string meshname = tile.gameObject.GetComponent<MeshFilter>().sharedMesh.name;
                MeshFilter mf = tile.gameObject.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    Destroy(tile.gameObject.GetComponent<MeshFilter>().sharedMesh);
                }
                Destroy(tiles[tileKey].gameObject);
            }
        }
        
        private IEnumerator DownloadBinaryMesh(TileChange tileChange, System.Action<TileChange> callback = null)
        {
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
            int index = tiles[tileKey].unityLOD;
            string url = Datasets[index].path;
            if (Datasets[index].path.StartsWith("https://") || Datasets[index].path.StartsWith("file://"))
            {
                //On WebGL we request brotli encoded files instead. We might want to base this on browser support.
#if !UNITY_EDITOR && UNITY_WEBGL
		        if(brotliCompressedExtention.Length>0 && !Datasets[index].path.EndsWith(brotliCompressedExtention))
    				Datasets[index].path += brotliCompressedExtention;
#endif
                url = Datasets[index].url;
            }
            url = url.ReplaceXY(tileChange.X, tileChange.Y);

            var webRequest = UnityWebRequest.Get(url);
#if !UNITY_EDITOR && UNITY_WEBGL && ADD_BROTLI_ACCEPT_ENCODING_HEADER
			webRequest.SetRequestHeader("Accept-Encoding", "br");
#endif

            tiles[tileKey].runningWebRequest = webRequest;
            yield return webRequest.SendWebRequest();

            if (!tiles.ContainsKey(tileKey)) yield break;

            tiles[tileKey].runningWebRequest = null;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                RemoveGameObjectFromTile(tileKey);
                callback(tileChange);
            }
            else
            {
                byte[] results = webRequest.downloadHandler.data;

                yield return new WaitUntil(() => pauseLoading == false);
                GameObject newGameobject = CreateNewGameObject(url, results, tileChange);
                if (newGameobject != null)
                {
#if SUBOBJECT
                    if (hasMetaData)
                    {
                        yield return StartCoroutine(LoadMetaData(newGameobject, url));
                    }
#endif

                    tiles[tileKey].gameObject = newGameobject;

                        callback(tileChange);
                    
                }
                else
                {
                    callback(tileChange);
                }
            }
        }



#if SUBOBJECT
        private IEnumerator LoadMetaData(GameObject gameObject, string geometryUrl)
        {

            var metaDataName = geometryUrl.Replace(".bin", "-data.bin");

            var webRequest = UnityWebRequest.Get(metaDataName);

            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                
                
            }
            else
            {
                if (gameObject == null) yield return null;

                byte[] results = webRequest.downloadHandler.data;
                ReadMetaDataFile(results, gameObject);
                yield return null;
            }
            yield return null;
        }

        private void ReadMetaDataFile(byte[] results, GameObject gameobject)
        {
            ObjectMapping objectMapping = gameobject.AddComponent<ObjectMapping>();
            objectMapping.items = new List<ObjectMappingItem>();
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

                        if (removeFromID.Length > 0)
                            id = id.Replace(removeFromID, "");

                        objectMapping.items.Add(new ObjectMappingItem()
                        {
                            objectID = id,
                            firstVertex = firstVertex,
                            verticesLength = vertexCount,
                        });
                    }
                }
            }
        }
#endif
        public void EnableShadows(bool enabled)
        {
            tileShadowCastingMode = (enabled) ? ShadowCastingMode.On : ShadowCastingMode.Off;

            MeshRenderer[] existingTiles = GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in existingTiles)
            {
                renderer.shadowCastingMode = tileShadowCastingMode;
            }
        }

        

        private GameObject CreateNewGameObject(string source, byte[] binaryMeshData, TileChange tileChange)
        {
            container = new GameObject();

            container.name = tileChange.X.ToString() + "-" + tileChange.Y.ToString();
            container.transform.position = CoordinateConverter.RDtoUnity(new Vector2(tileChange.X + (tileSize / 2), tileChange.Y + (tileSize / 2)));

            container.SetActive(isEnabled);

            mesh = BinaryMeshConversion.ReadBinaryMesh(binaryMeshData, out int[] submeshIndices);

#if !UNITY_EDITOR && UNITY_WEBGL
		    if(brotliCompressedExtention.Length>0 && source.EndsWith(brotliCompressedExtention))
				source = source.Replace(brotliCompressedExtention,"");
#endif
            mesh.name = source;
            container.AddComponent<MeshFilter>().sharedMesh = mesh;

            container.transform.parent = transform.gameObject.transform; //set parent after adding meshFilter to not cause SubObjects to give a missing component exception
            container.layer = container.transform.parent.gameObject.layer;

            meshRenderer = container.AddComponent<MeshRenderer>();
            List<Material> materialList = new List<Material>();
            for (int i = 0; i < submeshIndices.Length; i++)
            {
                materialList.Add(DefaultMaterialList[submeshIndices[i]]);
            }
            meshRenderer.sharedMaterials = materialList.ToArray();
            meshRenderer.shadowCastingMode = tileShadowCastingMode;

            if (createMeshcollider)
            {
                container.AddComponent<MeshCollider>().sharedMesh = mesh;
            }

            return container;
        }

        /// <summary>
        /// Adds mesh colliders to the meshes found within this layer
        /// </summary>
        /// <param name="onlyTileUnderPosition">Optional world position where this tile should be close to</param>
        public void AddMeshColliders(Vector3 onlyTileUnderPosition = default)
        {
            MeshCollider meshCollider;
            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

            if (meshFilters != null)
            {
                if (onlyTileUnderPosition != default)
                {
                    foreach (MeshFilter meshFilter in meshFilters)
                    {
                        if (Mathf.Abs(onlyTileUnderPosition.x - meshFilter.gameObject.transform.position.x) < tileSize && Mathf.Abs(onlyTileUnderPosition.z - meshFilter.gameObject.transform.position.z) < tileSize)
                        {
                            meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
                            if (meshCollider == null)
                            {
                                meshFilter.gameObject.AddComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
                            }
                        }
                    }
                    return;
                }

                //Just add all MeshColliders if no specific area was supplied
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
                    if (meshCollider == null)
                    {
                        meshFilter.gameObject.AddComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
                    }
                }
            }
        }
    }
}
