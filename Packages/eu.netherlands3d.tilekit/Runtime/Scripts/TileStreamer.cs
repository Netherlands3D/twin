using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Tilekit.BinaryMesh;
using UnityEngine;
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

        private void GetVisibleTiles()
        {
            var wPosA = GetCameraHitPointPosition(activeCamera, new Vector2(0, 0));
            var wPosB = GetCameraHitPointPosition(activeCamera, new Vector2(0, 1));
            var wPosC = GetCameraHitPointPosition(activeCamera, new Vector2(1, 1));
            var wPosD = GetCameraHitPointPosition(activeCamera, new Vector2(1, 0));

            var coordA = new Coordinate(wPosA).Convert(CoordinateSystem.RD);
            var coordB = new Coordinate(wPosB).Convert(CoordinateSystem.RD);
            var coordC = new Coordinate(wPosC).Convert(CoordinateSystem.RD);
            var coordD = new Coordinate(wPosD).Convert(CoordinateSystem.RD);

            var viewCorners = new Coordinate[]{ coordA, coordB, coordC, coordD };
            
            Debug.DrawLine(coordA.ToUnity(), coordB.ToUnity());
            Debug.DrawLine(coordB.ToUnity(), coordC.ToUnity());
            Debug.DrawLine(coordC.ToUnity(), coordD.ToUnity());
            Debug.DrawLine(coordD.ToUnity(), coordA.ToUnity());

            var minEasting = double.PositiveInfinity;
            var minNorthing = double.PositiveInfinity;
            var maxEasting = double.NegativeInfinity;
            var maxNorthing = double.NegativeInfinity;

            for (var i = 0; i < viewCorners.Length; i++)
            {
                var rd = viewCorners[i].Convert(CoordinateSystem.RD);

                minEasting = Math.Min(minEasting, rd.easting);
                minNorthing = Math.Min(minNorthing, rd.northing);
                maxEasting = Math.Max(maxEasting, rd.easting);
                maxNorthing = Math.Max(maxNorthing, rd.northing);
            }

            var tileRects = new List<RectInt>();

            var rects = new List<RectInt>();
            
            /*
            for (var x = (int)(minEasting / 1000) * 1000; x < maxEasting; x += 1000)
            {
                for (var y = (int)(minNorthing / 1000) * 1000; y < maxNorthing; y += 1000)
                {
                    rects.Add(new RectInt(x, y, 1000, 1000));
                    
                    //How much screenspace?
                    
                }
            }*/

            foreach (var rect in rects)
            {
                var a = new Coordinate(CoordinateSystem.RD, rect.xMin, rect.yMin);
                var b = new Coordinate(CoordinateSystem.RD, rect.xMin, rect.yMax);
                var c = new Coordinate(CoordinateSystem.RD, rect.xMax, rect.yMax);
                var d = new Coordinate(CoordinateSystem.RD, rect.xMax, rect.yMin);
                
                Debug.DrawLine(a.ToUnity(), b.ToUnity(), Color.yellow);
                Debug.DrawLine(b.ToUnity(), c.ToUnity(), Color.yellow);
                Debug.DrawLine(c.ToUnity(), d.ToUnity(), Color.yellow);
                Debug.DrawLine(d.ToUnity(), a.ToUnity(), Color.yellow);
            }

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
