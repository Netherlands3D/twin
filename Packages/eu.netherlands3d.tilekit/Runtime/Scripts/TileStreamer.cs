using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Tilekit.BinaryMesh;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Networking;

namespace Netherlands3D.Tilekit
{
    public class TileStreamer : MonoBehaviour
    {
        public bool IsStreaming { get; private set; }
        
        private readonly HashSet<Vector2Int> loadingTiles = new();
        private readonly Dictionary<Vector2Int, GameObject> loadedTiles = new();
        private readonly Dictionary<Vector2Int, UnityWebRequest> runningRequests = new();

        private Camera activeCamera;
        private Vector2Int? focusedTile;

        private int[] tileSizes = new int[]{4000, 2000, 1000, 500, 250, 125};

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
            focusedTile = null;
            
            foreach (var request in runningRequests.Values)
                request.Abort();
            
            
            StopAllCoroutines();

            runningRequests.Clear();
            loadingTiles.Clear();
        }

        private void Update()
        {
            if (!IsStreaming) return;
            GetVisibleTiles();
            var tileCoordinate = GetTileCoordinate(GetCenterCameraHitPointPosition(activeCamera));
            if (focusedTile.HasValue && focusedTile.Value == tileCoordinate) return;
            
            var previousFocusedTile = focusedTile;
            focusedTile = tileCoordinate;
            //LoadTile(tileCoordinate);
            if (previousFocusedTile.HasValue)
            {
                //UnloadTile(previousFocusedTile.Value);
            }
        }

        private static readonly Vector2 TopLeftScreenPoint = new Vector2(0, 1);
        private static readonly Vector2 TopRightScreenPoint = new Vector2(1, 1);
        private static readonly Vector2 BottomRightScreenPoint = new Vector2(1, 0);
        private static readonly Vector2 BottomLeftScreenPoint = new Vector2(0, 0);

        private readonly List<Vector3> visibleGroundPolygon = new(4);
        private readonly List<Tile> visibleTiles = new(64);

        private void GetVisibleTiles()
        {
            //TileMath.GetVisibleGroundPolygonNonAlloc_1(activeCamera, 10000f, visibleGroundPolygon);
            //v//ar flattenedGroundPolygon = visibleGroundPolygon.Select(e => new Vector3(e.x, 0, e.z)).ToList();
            //DrawShape(visibleGroundPolygon, Color.green);
            //DrawShape(flattenedGroundPolygon, Color.red);
            TileMath.GetVisibleGroundPolygonNonAlloc_2(activeCamera, 5000f, visibleGroundPolygon);
            //DrawShape(visibleGroundPolygon, Color.green);
            //DrawShapeFlat(visibleGroundPolygon, Color.green);
            DrawShape(visibleGroundPolygon, Color.blue);

            TileMath.GetVisibleTilesNonAlloc(activeCamera, 10000f, 8000, visibleTiles);

            foreach (var visibleTile in visibleTiles)
            {
                DrawTile(visibleTile, Color.red);
            }

        }

        private static void DrawTile(Tile tile, Color color)
        {
            var bounds = TileMath.GetBounds(tile);
            DrawBounds(bounds, color);
        }

        private static void DrawBounds(Bounds bounds, Color color)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            
            var bottomPoints = new List<Vector3>()
            {
                new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
            };
            DrawShape(bottomPoints, color);
            var topPoints = new List<Vector3>()
            {
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
            };
            DrawShape(topPoints, color);
            
            DrawShape(new List<Vector3>() {new Vector3(bounds.min.x, bounds.min.y, bounds.min.z), new Vector3(bounds.min.x, bounds.min.y, bounds.max.z)}, color);
            DrawShape(new List<Vector3>() {new Vector3(bounds.min.x, bounds.max.y, bounds.min.z), new Vector3(bounds.min.x, bounds.max.y, bounds.max.z)}, color);
            DrawShape(new List<Vector3>() {new Vector3(bounds.max.x, bounds.max.y, bounds.min.z), new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)}, color);
            DrawShape(new List<Vector3>() {new Vector3(bounds.max.x, bounds.min.y, bounds.min.z), new Vector3(bounds.max.x, bounds.min.y, bounds.max.z)}, color);
            
        }

        private static void DrawShape(List<Vector3> points, Color color)
        {
            for (var i = 0; i < points.Count; i++)
            {
                Debug.DrawLine(
                    points[i],
                    points[(i + 1) % points.Count],
                    color);
            }
        }
        
        private static void DrawShapeFlat(List<Vector3> points, Color color)
        {
            for (var i = 0; i < points.Count; i++)
            {
                var pointA = points[i];
                pointA.y = 0;
                var pointB = points[(i + 1) % points.Count];
                pointB.y = 0;
                Debug.DrawLine(
                    pointA,
                    pointB,
                    color);
                Debug.DrawRay(points[0], Vector3.up * 100000);
            }
        }

        private Vector3 GetCameraRayTerrainIntersectionPoint(Camera camera, Vector2 screenPosition, float terrainHeight, float maxViewingDistance)
        {
            //not correct when exceeding maxViewDistance. need intersections with near and far camera plane. Good enough for the experiment now.
            var ray = camera.ViewportPointToRay(screenPosition);
            var groundPlane = new Plane(Vector3.up, Vector3.one * terrainHeight);
            var rayPoint = ray.GetPoint(groundPlane.Raycast(ray, out var distance) ? Mathf.Min(distance, maxViewingDistance) : maxViewingDistance);
            return new Vector3(rayPoint.x, terrainHeight, rayPoint.z);
        }

        private Vector2 GetTerrainPoint(Vector3 worldPoint)
        {
            return new Vector2(worldPoint.x, worldPoint.z);
        }
        
        
        private Vector2Int GetTileCoordinate(Vector3 unityPosition, int tileSize = 1000)
        {
            var rdPosition = new Coordinate(unityPosition).Convert(CoordinateSystem.RD);
            var tileCoordinate = new Vector2Int(
                (int)System.Math.Round(rdPosition.easting / tileSize) * tileSize,
                (int)System.Math.Round(rdPosition.northing / tileSize) * tileSize
                );
            return tileCoordinate;
        }

        private Vector3 GetCenterCameraHitPointPosition(Camera camera)
        {
            return GetCameraHitPointPosition(camera, Vector2.one * .5f);
        }
        
        private Vector3 GetCameraHitPointPosition(Camera camera, Vector2 screenPoint)
        {
            var ray = camera.ViewportPointToRay(
                screenPoint
            );

            var groundPlane = new Plane(Vector3.up, Vector3.zero);
            
            var hitPoint = ray.GetPoint(groundPlane.Raycast(ray, out var distance) ? distance : 10_000f);
            
            //Debug.DrawRay(ray.origin,  ray.direction * distance, Color.red, .2f);
            //Debug.DrawRay(hitPoint,  Vector3.up * 1000, Color.green, .2f);

            return hitPoint;
        }
        
        private void LoadTile(Vector2Int tileCoordinate)
        {
            if (!IsStreaming) return;
            if (loadedTiles.ContainsKey(tileCoordinate)) return;
            if (!loadingTiles.Add(tileCoordinate)) return;
            StartCoroutine(LoadTileCoroutine(tileCoordinate));
        }

        private void UnloadTile(Vector2Int tileCoordinate)
        {
            if (!loadedTiles.Remove(tileCoordinate, out var tileObject))
                return;
            
            var meshFilter = tileObject.GetComponent<MeshFilter>();

            if (meshFilter && meshFilter.sharedMesh)
                Destroy(meshFilter.sharedMesh);

            Destroy(tileObject);
        }

        private IEnumerator LoadTileCoroutine(Vector2Int tileCoordinate)
        {
            Debug.Log($"Load tile {tileCoordinate.x},{tileCoordinate.y}");
            
            var url = GetUrl(tileCoordinate);
            using var request = UnityWebRequest.Get(url);
            runningRequests.Add(tileCoordinate, request);
            GameObject tileObject = null;

            try
            {
                yield return request.SendWebRequest();
                if (!IsStreaming)
                {
                    yield break;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load tile {tileCoordinate}: " + $"{request.error}\n{url}");
                    yield break;
                }

                var data = request.downloadHandler.data;
                
                yield return RunCoroutine<GameObject>(CreateTileCoroutine(tileCoordinate, url, data), result => tileObject = result);
            }
            finally
            {
                loadingTiles.Remove(tileCoordinate);
                runningRequests.Remove(tileCoordinate);
                loadedTiles.Add(tileCoordinate, tileObject);
            }
        }

        private IEnumerator CreateTileCoroutine(Vector2Int tileCoordinate, string url, byte[] data)
        {
            var tileObject = new GameObject($"YEAH! {tileCoordinate.x},{tileCoordinate.y}");
            tileObject.layer = gameObject.layer;
            
            tileObject.transform.position = new Coordinate(
                CoordinateSystem.RD,
                tileCoordinate.x,
                tileCoordinate.y
            ).ToUnity();

            var readResult = BinaryMeshReader.FromData(data);
            var mesh = readResult.Item1;
            var materialIndices = readResult.Item2;
            
            tileObject.transform.position = new Coordinate(
                CoordinateSystem.RD,
                tileCoordinate.x,
                tileCoordinate.y
            ).ToUnity();
            
            tileObject.transform.SetParent(transform, true);

            //tileObject.transform.localScale = new Vector3(1000, 100, 1000);

            mesh.name = url;
            
            var meshFilter = tileObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            
            var meshRenderer = tileObject.AddComponent<MeshRenderer>();
            var m = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.materials = new Material[] { m, m, m, m, m, m, m, m, m, m, m, m, m };
            
            yield return tileObject;
        }

        private string GetUrl(Vector2Int tileCoordinate)
        {
            var x = tileCoordinate.x;
            var y = tileCoordinate.y;
            return
                $"https://assets.netherlands3d.eu/publicassets/Terrain_2019-despiked_simplified100/Terrain{x}_{y}.1.100.bin";
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
