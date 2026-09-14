using System;
using System.IO;
using System.Threading.Tasks;
using Netherlands3D.CartesianTiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Netherlands3D.Editor.UnityTerrainTiles
{
    public static class MaaiveldUnityTerrainTilesGenerator
    {
        private const int TileSize = 1000;
        
        private const string MeshDataBaseUrl = "https://assets.netherlands3d.eu/publicassets/Terrain_2019-despiked/";
        private const string MeshDataFileNamePattern = "Terrain{0}_{1}.1.bin";
        private static readonly string TerrainCacheFolder = Path.Combine(Application.temporaryCachePath, "UnityTerrainTiles");
        private static readonly string MeshDataCacheFolder = Path.Combine(TerrainCacheFolder, "MeshData");
        private static readonly string TerrainMapCacheFolder = Path.Combine(TerrainCacheFolder, "Terrain");

        [MenuItem("Netherlands3D/Generate maaiveld terrain map tiles for Arnhem...")]
        public static async void ProcessArnhem()
        {
            try
            {
                await ProcessArnhemAsync();
            }
            catch (Exception e)
            {
                throw; // TODO handle exception
            }
        }

        

        private static async Task ProcessArnhemAsync()
        {
            var area = new RectInt(new Vector2Int(188000, 444000), new Vector2Int(6000, 6000));
            var tileSize = 1000;
            await ProcessTilesAsync(area, tileSize);
        }
        
        private static async Task ProcessTilesAsync(RectInt area, int tileSize)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            try
            {
                

                var tileCount = 0;
                var tileCountTotal = (area.width/tileSize) * (area.height/tileSize);
                var cancel = false;
                
                for (var y = area.yMin; y < area.yMax; y += tileSize)
                {
                    for (var x = area.xMin; x < area.xMax; x += tileSize)
                    {
                        tileCount++;
                        var title = "Generating tiles...";
                        var description = $"{tileCount}/{tileCountTotal}";
                        var progress = tileCount / (float)tileCountTotal;
                        if (EditorUtility.DisplayCancelableProgressBar(title, description, progress))
                        {
                            return;
                        }
                        await ProcessTileAsync(x, y, scene);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                EditorUtility.ClearProgressBar();
            }
            
        }

        private static async Task ProcessTileAsync(int x, int y, Scene scene)
        {
            var url = GetMeshDataUrl(x, y);
            var meshData = await GetMeshDataAsync(url);

            var gameObject = CreateMeshCollider(meshData);

            SceneManager.MoveGameObjectToScene(
                gameObject,
                scene);
            
            var oldValue = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            try
            {
                GenerateTerrainMap(x, y, gameObject);
            }
            finally
            {
                Physics.queriesHitBackfaces = oldValue;
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static string GetMeshDataUrl(int x, int y)
        {
            var fileName = string.Format(MeshDataFileNamePattern, x, y);
            return MeshDataBaseUrl + fileName;
        }

        private static async Task<byte[]> GetMeshDataAsync(string url)
        {
            Directory.CreateDirectory(MeshDataCacheFolder);

            var fileName = Path.GetFileName(url);

            var localPath = Path.Combine(MeshDataCacheFolder, fileName);

            if (File.Exists(localPath))
            {
                Debug.Log($"Loading cached mesh: {fileName}");

                return await File.ReadAllBytesAsync(localPath);
            }

            Debug.Log($"Downloading mesh: {url}");

            using var request = UnityWebRequest.Get(url);

            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"Download failed: " + $"{request.responseCode} " + $"{request.error}\n{url}");
            }

            var data = request.downloadHandler.data;

            await File.WriteAllBytesAsync(localPath, data);

            return data;
        }

        private static GameObject CreateMeshCollider(byte[] data)
        {
            var mesh = BinaryMeshConversion.ReadBinaryMesh(data, out _);
            var gameObject = new GameObject("MaaiveldTile");
            
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();

            var material = new Material(
                Shader.Find("Universal Render Pipeline/Lit"));

            var materials = new Material[mesh.subMeshCount];

            for (var i = 0; i < materials.Length; i++)
                materials[i] = material;

            meshRenderer.sharedMaterials = materials;

            var collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
             
            return gameObject;
        }

        private static void GenerateTerrainMap(int tileX, int tileY, GameObject gameObject)
        {
            const int resolution = 513;
            const float rayStartOffset = 10f;

            Directory.CreateDirectory(TerrainMapCacheFolder);

            var collider = gameObject.GetComponent<MeshCollider>();
            var bounds = collider.bounds;

            var minHeight = bounds.min.y;
            var maxHeight = bounds.max.y;

            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

            var pixels = new Color32[resolution * resolution];

            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var normalizedX = x / (float)(resolution - 1);
                    var normalizedY = y / (float)(resolution - 1);

                    var worldX = Mathf.Lerp(bounds.min.x, bounds.max.x, normalizedX);
                    var worldZ = Mathf.Lerp(bounds.min.z, bounds.max.z, normalizedY);

                    var rayOrigin = new Vector3(worldX, bounds.max.y + rayStartOffset, worldZ);
                    
                    
                    var ray = new Ray(rayOrigin, Vector3.down);
                    


                    if (!collider.Raycast(ray, out var hit, bounds.size.y + rayStartOffset))
                    {
                        continue;
                    }

                    var normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, hit.point.y);

                    var heightByte = (byte)Mathf.RoundToInt(normalizedHeight * 255f);

                    pixels[y * resolution + x] = new Color32(heightByte, 0, 0, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var fileName = $"Terrainmap{tileX}_{tileY}.png";

            var path = Path.Combine(TerrainMapCacheFolder, fileName);

            File.WriteAllBytes(path, texture.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(texture);

            Debug.Log($"Saved {path} " + $"(min {minHeight:F2}, max {maxHeight:F2})");
        }
    }
}