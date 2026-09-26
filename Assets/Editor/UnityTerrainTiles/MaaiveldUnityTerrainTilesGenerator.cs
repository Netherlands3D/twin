using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Netherlands3D.CartesianTiles;
using NetTopologySuite.Operation.Buffer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;

namespace Netherlands3D.Editor.UnityTerrainTiles
{
    public static class MaaiveldUnityTerrainTilesGenerator
    {
        private const int SourceTileSize = 1000;
        
        private const string MeshDataBaseUrl = "https://assets.netherlands3d.eu/publicassets/Terrain_2019-despiked/";
        private const string MeshDataFileNamePattern = "Terrain{0}_{1}.1.bin";
        private static readonly string TerrainCacheFolder = Path.Combine(Application.temporaryCachePath, "UnityTerrainTiles");
        private static readonly string SourceMeshDataCacheFolder = Path.Combine(TerrainCacheFolder, "SourceMeshData");
        private static readonly string TerrainMapCacheFolder = Path.Combine(TerrainCacheFolder, "Terrain");

        [MenuItem("Netherlands3D/Generate maaiveld terrain map tiles for Arnhem...")]
        public static async void ProcessArnhem()
        {
            //try
            {
                await ProcessArnhemAsync();
            }
            //catch (Exception e)
            //{/
             //   throw new Exception();
            //}
        }

        

        private static async Task ProcessArnhemAsync()
        {
            var rdArea = new RectInt(new Vector2Int(186000, 444000), new Vector2Int(3000, 3000));
            var tileSizes = new int[] { 1000, 2000, 4000, 8000 };
            await ProcessTilesAsync(rdArea, tileSizes);
        }
        
        private static async Task ProcessTilesAsync(RectInt totalArea, int[] tileSizes)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            try
            {

                var cancel = false;

                foreach (var tileSize in tileSizes)
                {
                    var tileCount = 0;
                    var tileCountTotal = (totalArea.width/tileSize) * (totalArea.height/tileSize);
                    
                    for (var y = totalArea.yMin; y < totalArea.yMax; y += tileSize)
                    {
                        for (var x = totalArea.xMin; x < totalArea.xMax; x += tileSize)
                        {
                            tileCount++;
                            var title = "Generating tiles...";
                            var description = $"{tileCount}/{tileCountTotal} {tileSize}";
                            var progress = tileCount / (float)tileCountTotal;
                            if (EditorUtility.DisplayCancelableProgressBar(title, description, progress))
                            {
                                return;
                            }

                            var tileArea = new Rect(x, y, tileSize, tileSize);

                            await ProcessTileAsync(tileArea, scene);
                        }
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

        private static async Task ProcessTileAsync(Rect tileArea, Scene scene)
        {


            var sourceGameObjects = new List<GameObject>();
            
            var minX = Mathf.Floor(tileArea.xMin / (float)SourceTileSize) * SourceTileSize;
            var minY = Mathf.Floor(tileArea.yMin / (float)SourceTileSize) * SourceTileSize;
            var maxX = Mathf.Ceil(tileArea.xMax / (float)SourceTileSize) * SourceTileSize;
            var maxY = Mathf.Ceil(tileArea.yMax / (float)SourceTileSize) * SourceTileSize;

            for (var x = minX; x < maxX; x += SourceTileSize)
            {
                for (var y = minY; y < maxY; y += SourceTileSize)
                {
                    var sourceUrl = GetSourceMeshDataUrl(x, y);
                    var sourceMeshData = await GetSourceMeshDataAsync(sourceUrl);
                    var sourceGameObject = CreateMeshCollider(sourceMeshData, $"source {x},{y}");
                    SceneManager.MoveGameObjectToScene(sourceGameObject, scene);
                    sourceGameObject.transform.position = new Vector3(x - tileArea.position.x + 500, 0, y - tileArea.position.y + 500);
                    sourceGameObjects.Add(sourceGameObject);
                }
            }
            
            
            await Awaitable.NextFrameAsync();

            var oldValue = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            List<GameObject> tempGameObjects = new List<GameObject>();

            try
            {
                var bytes = GenerateTerrainMap(tileArea, sourceGameObjects);
                PngPreviewWindow.Open(bytes);
                await Task.Delay(1000);
                /*
                var fileName = $"Terrainmap{tileArea.width}_{tileArea.x}_{tileArea.y}.png";
                var path = Path.Combine(TerrainMapCacheFolder, fileName);
                await File.WriteAllBytesAsync(path, bytes);*/

                var combinedMesh = GenerateCombinedMesh(tileArea, sourceGameObjects);
                var previewCombinedMesh = CreateMeshCollider(combinedMesh, "Combined");
                previewCombinedMesh.transform.position = Vector3.up * 2000;
                tempGameObjects.Add(previewCombinedMesh);
                await Task.Delay(1000);
                //todo: create cutout

                for (int i = 0; i <= 20; i++)
                {
                    var meshSimplifier = new MeshSimplifier(combinedMesh);
                    var quality = (20f - i) / 20f;
                    
                    var options = meshSimplifier.SimplificationOptions;
                    options.Agressiveness = 30;
                    meshSimplifier.SimplificationOptions = options;
                    
                    meshSimplifier.SimplifyMesh(quality);
                    var simplifiedMesh = meshSimplifier.ToMesh();
                    Debug.Log($"Quality: {quality}, vertexCount:{simplifiedMesh.vertexCount}");
                    var previwesimplifiedMesh = CreateMeshCollider(simplifiedMesh, "Simplified"+i);
                    previwesimplifiedMesh.transform.position = Vector3.up * (2000 + i * 1000);
                    tempGameObjects.Add(previwesimplifiedMesh);
                    await Task.Delay(1000);
                }
                
                await Task.Delay(5000);
            }
            finally
            {
                Physics.queriesHitBackfaces = oldValue;
                
                foreach (var go in tempGameObjects)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
                
                foreach (var go in sourceGameObjects)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            
            
            
        }

        private static string GetSourceMeshDataUrl(float x, float y)
        {
            var fileName = string.Format(MeshDataFileNamePattern, x, y);
            return MeshDataBaseUrl + fileName;
        }

        private static async Task<byte[]> GetSourceMeshDataAsync(string url)
        {
            Directory.CreateDirectory(SourceMeshDataCacheFolder);

            var fileName = Path.GetFileName(url);

            var localPath = Path.Combine(SourceMeshDataCacheFolder, fileName);

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

        private static GameObject CreateMeshCollider(byte[] data, string name = null)
        {
            var mesh = BinaryMeshConversion.ReadBinaryMesh(data, out _);
            return CreateMeshCollider(mesh);
        }

        private static GameObject CreateMeshCollider(Mesh mesh, string name = null)
        {
            var gameObject = new GameObject(name);
            
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
            
            mesh.RecalculateBounds();
             
            return gameObject;
        }

        private static byte[] GenerateTerrainMap(Rect tileArea, List<GameObject> sourceGameObjects)
        {
            const int resolution = 513;
            const float rayStartOffset = 10f;

            Directory.CreateDirectory(TerrainMapCacheFolder);
            
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

            var pixels = new Color32[resolution * resolution];

            var sourceBoundsCollection = sourceGameObjects.Select(e => e.GetComponent<MeshCollider>().bounds);

            var tileBounds = new Bounds();

            foreach (var sourceBounds in sourceBoundsCollection)
            {
                tileBounds.Encapsulate(sourceBounds);
            }
            

            foreach (var sourceGameObject in sourceGameObjects)
            {
                var collider = sourceGameObject.GetComponent<MeshCollider>();
                
                var minHeight = tileBounds.min.y;
                var maxHeight = tileBounds.max.y;

                for (var pixelY = 0; pixelY < resolution; pixelY++)
                {
                    for (var pixelX = 0; pixelX < resolution; pixelX++)
                    {
                        var normalizedX = pixelX / (float)(resolution - 1);
                        var normalizedY = pixelY / (float)(resolution - 1);

                        var worldX = Mathf.Lerp(0, tileArea.width, normalizedX);
                        var worldZ = Mathf.Lerp(0, tileArea.height, normalizedY);

                        var rayOrigin = new Vector3(worldX, tileBounds.max.y + rayStartOffset, worldZ);

                        var ray = new Ray(rayOrigin, Vector3.down);

                        if (!collider.Raycast(ray, out var hit, tileBounds.size.y + rayStartOffset))
                        {
                            continue;
                        }

                        var normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, hit.point.y);

                        var heightByte = (byte)Mathf.RoundToInt(normalizedHeight * 255f);

                        pixels[pixelY * resolution + pixelX] = new Color32(heightByte, 0, 0, 255);
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var bytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            /*
            var fileName = $"Terrainmap{tileArea.width}_{tileArea.x}_{tileArea.y}.png";

            var path = Path.Combine(TerrainMapCacheFolder, fileName);

            File.WriteAllBytes(path, texture.EncodeToPNG());

            

            Debug.Log($"Saved {path}");*/

            return bytes;
        }

        private static Mesh GenerateCombinedMesh(Rect tileArea, List<GameObject> sourceGameObjects)
        {
            var combineInstances = new List<CombineInstance>();
            
            for (var i = 0; i < sourceGameObjects.Count; i++)
            {
                var meshFilter = sourceGameObjects[i].GetComponent<MeshFilter>();
                var meshRenderer =  sourceGameObjects[i].GetComponent<MeshRenderer>();
                var mesh = meshFilter.sharedMesh;
                Matrix4x4 transform = sourceGameObjects[i].transform.localToWorldMatrix;

                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    var combineInstance = new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = subMesh,
                        transform = transform
                    };
                    combineInstances.Add(combineInstance);
                }
                
            }
            var combinedMesh = new Mesh
            {
                name = "Combined",
                indexFormat = IndexFormat.UInt32
            };
            combinedMesh.CombineMeshes(combineInstances.ToArray(), false, true);
            return combinedMesh;
          
        }
        
        
    }
}