using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Netherlands3D.Tilekit
{
    public class TileStreamer : MonoBehaviour
    {
        
        private const int rootTileSize = 2000;
        private const int minTileSize = 250;

        [SerializeField] private float splitThreshold = 500f;
        
        public bool IsStreaming { get; private set; }
        private Camera activeCamera;

        private readonly Dictionary<Tile, UnityWebRequest> runningRequests = new();

        private readonly TileSelector tileSelector = new();
        private readonly List<Tile> desiredTilesList = new(256);
        
        private readonly List<Tile> _debug_desiredTilesList = new(256);
        
        private readonly HashSet<Tile> desiredTiles = new();
        private readonly HashSet<Tile> loadingTiles = new();
        private readonly Dictionary<Tile, GameObject> loadedTiles = new();
        private readonly HashSet<Tile> visibleTiles = new();
        private readonly HashSet<Tile> splitTiles = new();
        
        private readonly HashSet<Tile> requiredTiles = new();
        private readonly HashSet<Tile> currentRootTiles = new();
        private readonly HashSet<Tile> activeRootTiles = new();
        private readonly List<Tile> rootsToUnload = new();


        private void OnDrawGizmos()
        {
            DrawGizmosDesiredTiles();
            DrawGizmosCameraFrustum(activeCamera);
        }

        private void DrawGizmosDesiredTiles()
        {
            foreach (var tile in _debug_desiredTilesList)
            {
                var tileBounds = TileMath.GetUnityBounds(tile);
                Gizmos.DrawWireCube(tileBounds.center, tileBounds.size);
            }
        }
        
        private static void DrawGizmosCameraFrustum(Camera camera)
        {
            if (!camera)
                return;

            var previousMatrix = Gizmos.matrix;
            var previousColor = Gizmos.color;

            Gizmos.color = Color.yellow;
            Gizmos.matrix = Matrix4x4.TRS(
                camera.transform.position,
                camera.transform.rotation,
                Vector3.one
            );

            if (camera.orthographic)
            {
                var depth = camera.farClipPlane - camera.nearClipPlane;
                var height = camera.orthographicSize * 2f;
                var width = height * camera.aspect;

                Gizmos.DrawWireCube(
                    new Vector3(0f, 0f, camera.nearClipPlane + depth * 0.5f),
                    new Vector3(width, height, depth)
                );
            }
            else
            {
                Gizmos.DrawFrustum(
                    Vector3.zero,
                    camera.fieldOfView,
                    camera.farClipPlane,
                    camera.nearClipPlane,
                    camera.aspect
                );
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }


        public void StartStreaming()
        {
            if (IsStreaming) return;
            
            IsStreaming = true;
        }

        public void SetActiveCamera(Camera activeCamera)
        {
            this.activeCamera = activeCamera;
        }

        public void StopStreaming()
        {
            if (!IsStreaming) return;

            IsStreaming = false;
            
            foreach (var request in runningRequests.Values)
                request.Abort();
            
            StopAllCoroutines();

            runningRequests.Clear();
            loadingTiles.Clear();
        }

        private void Update()
        {
            if (!IsStreaming) return;
            
            tileSelector.GetDesiredTilesNonAlloc(activeCamera, activeCamera.farClipPlane, rootTileSize, minTileSize, splitThreshold, desiredTilesList);
            
            //tileSelector.GetDesiredTilesNonAlloc(activeCamera, activeCamera.farClipPlane, 100, 100, splitThreshold, _debug_desiredTilesList);
            
            UpdateTileStreaming();

        }

        private void UpdateTileStreaming()
        {
            desiredTiles.Clear();
            requiredTiles.Clear();
            currentRootTiles.Clear();

            for (var i = 0; i < desiredTilesList.Count; i++)
            {
                var desiredTile = desiredTilesList[i];
                desiredTiles.Add(desiredTile);
                requiredTiles.Add(desiredTile);
                var ancestor = desiredTile;

                while (ancestor.Size < rootTileSize)
                {
                    ancestor = TileMath.GetParent(ancestor);
                    requiredTiles.Add(ancestor);
                }
                
                currentRootTiles.Add(ancestor);
            }
            
            // Find roots that are no longer required.
            rootsToUnload.Clear();

            foreach (var activeRoot in activeRootTiles)
            {
                if (!currentRootTiles.Contains(activeRoot))
                {
                    rootsToUnload.Add(activeRoot);
                }
            }
            
            // Completely unload roots that left the streaming area.
            for (var i = 0; i < rootsToUnload.Count; i++)
            {
                var root = rootsToUnload[i];

                UnloadTileBranch(root);
                activeRootTiles.Remove(root);
            }
            
            // Update every currently desired root independently.
            foreach (var root in currentRootTiles)
            {
                activeRootTiles.Add(root);
                UpdateTileBranch(root);
            }
        }
        
        private void UpdateTileBranch(Tile tile)
        {
            // This branch is no longer needed.
            if (!requiredTiles.Contains(tile))
            {
                UnloadTileBranch(tile);
                return;
            }

            // This tile is the final desired detail level.
            if (desiredTiles.Contains(tile))
            {
                // If it isn't loaded yet, keep any existing children visible
                // while the parent loads.
                if (!ShowTile(tile))
                {
                    LoadTile(tile);
                    return;
                }

                // The desired parent is now visible. Remove any children that
                // previously covered it or were staged for refinement.
                if (tile.Size > minTileSize)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var child = TileMath.GetChild(tile, i);

                        if (HasBranchState(child))
                        {
                            UnloadTileBranch(child);
                        }
                    }
                }

                splitTiles.Remove(tile);
                return;
            }

            // This parent has already been replaced by its children.
            if (splitTiles.Contains(tile))
            {
                for (var i = 0; i < 4; i++)
                {
                    UpdateTileBranch(TileMath.GetChild(tile, i));
                }

                return;
            }

            // Make sure the coarse parent exists before loading its children.
            if (!ShowTile(tile))
            {
                LoadTile(tile);
                return;
            }

            var allChildrenLoaded = true;

            for (var i = 0; i < 4; i++)
            {
                var child = TileMath.GetChild(tile, i);

                // This should normally always be true when the selector
                // returns a complete quadtree.
                if (!requiredTiles.Contains(child))
                {
                    allChildrenLoaded = false;
                    continue;
                }

                if (loadedTiles.ContainsKey(child))
                    continue;

                allChildrenLoaded = false;
                LoadTile(child);
            }

            // Keep showing the parent until all four children are ready.
            if (!allChildrenLoaded)
                return;

            // Perform the visual switch in one frame.
            for (var i = 0; i < 4; i++)
            {
                ShowTile(TileMath.GetChild(tile, i));
            }

            splitTiles.Add(tile);
            UnloadTile(tile);
        }
        
        private void UnloadTileBranch(Tile tile)
        {
            // Abort an unfinished download.
            if (runningRequests.TryGetValue(tile, out var request))
            {
                request.Abort();
            }

            UnloadTile(tile);
            splitTiles.Remove(tile);
            visibleTiles.Remove(tile);

            if (tile.Size <= minTileSize)
                return;

            for (var i = 0; i < 4; i++)
            {
                var child = TileMath.GetChild(tile, i);

                if (HasBranchState(child))
                {
                    UnloadTileBranch(child);
                }
            }
        }
        
        private bool ShowTile(Tile tile)
        {
            if (!loadedTiles.TryGetValue(tile, out var tileObject))
                return false;

            if (!tileObject)
                return false;

            tileObject.SetActive(true);
            visibleTiles.Add(tile);

            return true;
        }

        private bool HasBranchState(Tile tile)
        {
            return loadedTiles.ContainsKey(tile) ||
                   loadingTiles.Contains(tile) ||
                   splitTiles.Contains(tile);
        }

        private void LoadTile(Tile tile)
        {
            if (!IsStreaming) return;
            if (loadedTiles.ContainsKey(tile)) return;
            if (!loadingTiles.Add(tile)) return;
            StartCoroutine(LoadTileCoroutine(tile));
        }

        private void UnloadTile(Tile tile)
        {
            visibleTiles.Remove(tile);
            
            if (!loadedTiles.Remove(tile, out var tileGameObject))
                return;
            
            var meshFilter = tileGameObject.GetComponent<MeshFilter>();

            //if (meshFilter && meshFilter.sharedMesh)
                //Destroy(meshFilter.sharedMesh);

            Destroy(tileGameObject);
        }

        private IEnumerator LoadTileCoroutine(Tile tile)
        {
            var url = GetUrl(tile);
            using var request = UnityWebRequest.Get(url);
            runningRequests.Add(tile, request);
            GameObject tileObject = null;

            try
            {
                byte[] data = null;

                var randomNumber = UnityEngine.Random.Range(1f, 4f);
                yield return new WaitForSeconds(randomNumber);
                
                yield return RunCoroutine<GameObject>(CreateTileCoroutine(tile, url, data), result => tileObject = result);
            }
            finally
            {
                loadingTiles.Remove(tile);
                runningRequests.Remove(tile);
                
                if (tileObject != null && IsStreaming && requiredTiles.Contains(tile))
                {
                    loadedTiles.Add(tile, tileObject);
                }
                else if (tileObject != null)
                {
                    Destroy(tileObject);
                }
            }
        }

        private IEnumerator CreateTileCoroutine(Tile tile, string url, byte[] data)
        {
            var tileObject = new GameObject($"{tile}");
            tileObject.SetActive(false);
            tileObject.layer = gameObject.layer;
            tileObject.transform.SetParent(transform, true);
            tileObject.transform.position = new Coordinate(
                CoordinateSystem.RD,
                tile.RdX + tile.Size / 2f,
                tile.RdY + tile.Size / 2f 
            ).ToUnity();
            
            var meshFilter = tileObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            
            var meshRenderer = tileObject.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            meshRenderer.sharedMaterial = material;
            
            for (var i = 0f; i < 1; i += Time.deltaTime * 5f)
            {
                tileObject.transform.localScale = new Vector3(tile.Size, 100, tile.Size) * i;
                yield return null;
            }

            tileObject.transform.localScale = new Vector3(tile.Size, 100, tile.Size);
            
            /*
            var readResult = BinaryMeshReader.FromData(data);
            var mesh = readResult.Item1;
            var materialIndices = readResult.Item2;
            
            


            //tileObject.transform.localScale = new Vector3(1000, 100, 1000);

            mesh.name = url;
            
            var meshFilter = tileObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            
            var meshRenderer = tileObject.AddComponent<MeshRenderer>();
            var m = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.materials = new Material[] { m, m, m, m, m, m, m, m, m, m, m, m, m };

            for (var i = 0f; i < 1; i += Time.deltaTime / 4f)
            {
                tileObject.transform.localScale = i * Vector3.one;
                yield return null;
            }
            tileObject.transform.localScale = Vector3.one;
            */
            yield return tileObject;
        }

        private string GetUrl(Tile tile)
        {
            var x = tile.RdX;
            var y = tile.RdY;
            //return
                //$"https://assets.netherlands3d.eu/publicassets/Terrain_2019-despiked_simplified100/Terrain{x}_{y}.1.100.bin";

            return $"https://assets.netherlands3d.eu/publicassets/Terrain_2019-despiked/Terrain{x}_{y}.1.bin";
        }

        private void OnDisable()
        {
            StopStreaming();
        }
        
        public static IEnumerator RunCoroutine<T>(IEnumerator target, Action<T> output)
        {
            object result = null;
            while (target.MoveNext())
            {
                result = target.Current;
                yield return result;
            }
            output((T)result);
        }


        
        
    }
}
