/*
*  Copyright (C) X Gemeente
*                X Amsterdam
*                X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://github.com/Amsterdam/3DAmsterdam/blob/master/LICENSE.txt
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Netherlands3D.Coordinates;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Netherlands3D.CartesianTiles;
using System;

namespace Netherlands3D.Functionalities.Toponyms
{
    public class GeoJSONStreetLayerGameObject : GeoJSONTextLayer
    {
        private static Dictionary<Vector2Int, List<string>> uniqueNames = new Dictionary<Vector2Int, List<string>>();
        private static Dictionary<Vector2Int, List<StreetName>> streetNames = new Dictionary<Vector2Int, List<StreetName>>();
        private static Dictionary<Vector2Int, List<StreetName>> streetNamesUpdateQueue = new Dictionary<Vector2Int, List<StreetName>>();
        private Vector3 previousPosition;
        private Vector3 previousRotation;
        private Camera mainCamera;
        private float maxScale = 0.5f;

        public struct StreetName
        {
            public string name;
            public float textSize;
            public float splineLength;
            public float baseLineY;
            public TextMeshPro text;
            public Coordinate[] positions;
            public Vector3[] pathPoints;
            public Vector3[] vertices;
            public Vector2[] uvs;
            public Vector3[] originalVertices;
            public Vector2[] originalUvs;

            public StreetName(string name, TextMeshPro text, Coordinate[] positions, float textSize)
            {
                this.name = name;
                this.text = text;
                this.textSize = textSize;
                this.positions = positions;
                pathPoints = new Vector3[positions.Length];
                text.text = name;
                text.ForceMeshUpdate();
                var meshInfo = text.textInfo.meshInfo[0];
                originalVertices = meshInfo.vertices;
                originalUvs = new Vector2[meshInfo.uvs0.Length];
                var uv0array = meshInfo.uvs0;
                for (int i = 0; i < uv0array.Length; i++)
                    originalUvs[i] = uv0array[i];

                vertices = new Vector3[originalVertices.Length];
                uvs = new Vector2[originalUvs.Length];

                splineLength = 0f;
                for (int i = 0; i < positions.Length; i++)
                {
                    Vector3 c = positions[i].ToUnity();
                    c.y = 0;
                    pathPoints[i] = c;
                    if (i > 0)
                    {
                        float len = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
                        splineLength += len;
                    }
                }
                text.enabled = false;

                var textInfo = text.textInfo;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                baseLineY = float.MaxValue;
                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    var ci = textInfo.characterInfo[i];
                    if (!ci.isVisible) continue;

                    minY = Mathf.Min(minY, ci.bottomLeft.y);
                    maxY = Mathf.Max(maxY, ci.topLeft.y);
                    baseLineY = Mathf.Min(baseLineY, ci.baseLine);
                }             
            }
        }

        public override void Start()
        {
            base.Start();
            mainCamera = Camera.main;
            geoJsonUrl = "https://service.pdok.nl/rws/nwbwegen/wfs/v1_0?service=WFS&version=2.0.0&request=GetFeature&typeNames=nwbwegen:wegvakken&srsName=EPSG:4326&outputFormat=application/json&bbox=";
        }

        protected override void RemoveGameObjectFromTile(Vector2Int tileKey)
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

                RemoveStreetNames(tile, tileKey);
                Destroy(tiles[tileKey].gameObject);
            }
        }       

        private void RemoveStreetNames(Tile tile, Vector2Int tileKey)
        {
            MeshFilter mf = tile.gameObject.GetComponent<MeshFilter>();
            if (mf != null)
            {
                Destroy(tile.gameObject.GetComponent<MeshFilter>().sharedMesh);
            }
            if (streetNames.ContainsKey(tileKey))
                foreach (StreetName streetName in streetNames[tileKey])
                    Destroy(streetName.text.gameObject);
            streetNames.Remove(tileKey);
            streetNamesUpdateQueue.Remove(tileKey);
            uniqueNames.Remove(tileKey);
        }

        protected override IEnumerator DownloadTextNameData(TileChange tileChange, Tile tile, System.Action<TileChange> callback = null)
        {          
            string geomUrl = $"{geoJsonUrl}{tileChange.X},{tileChange.Y},{(tileChange.X + tileSize)},{(tileChange.Y + tileSize)}";
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
            var streetnameRequest = UnityWebRequest.Get(geomUrl);
            tile.runningWebRequest = streetnameRequest;
            yield return streetnameRequest.SendWebRequest();

            if (streetnameRequest.result == UnityWebRequest.Result.Success)
            {
                if (!uniqueNames.ContainsKey(tileKey))
                    uniqueNames.Add(tileKey, new List<string>());
                uniqueNames[tileKey].Clear();
                GeoJsonFeatureCollection featureCollection = JsonConvert.DeserializeObject<GeoJsonFeatureCollection>(streetnameRequest.downloadHandler.text);
                int parses = 0;
                foreach (var feature in featureCollection.features)
                {
                    string name = feature.properties["sttNaam"].ToString();
                    if (IsNamePresent(name))
                        continue;

                    uniqueNames[tileKey].Add(name);
                    var coordinatesToken = feature.geometry.coordinates as JArray;

                    List<Coordinate> positions = new();

                    if (coordinatesToken != null && coordinatesToken.Count > 0)
                    {
                        var innerArray = coordinatesToken[0] as JArray;
                        if (innerArray != null)
                        {
                            foreach (var pointToken in innerArray)
                            {
                                string pointString = pointToken.ToString();
                                var point = JArray.Parse(pointString);
                                if (point.Count == 2)
                                {
                                    double lon = point[0].Value<double>();
                                    double lat = point[1].Value<double>();
                                    Coordinate coord = new Coordinate(CoordinateSystem.WGS84, lat, lon, 0);
                                    positions.Add(coord);
                                }
                            }
                        }
                    }
                    Coordinate[] latLonPositions = positions.ToArray();
                    foreach (TextsAndSize textAndSize in textsAndSizes)
                    {
                        var textObject = Instantiate(textPrefab);
                        textObject.name = name;
                        textObject.transform.SetParent(tile.gameObject.transform, true);
                        TextMeshPro tmp = textObject.GetComponent<TextMeshPro>();
                        StreetName streetName = new StreetName(name, tmp, latLonPositions, textAndSize.drawWithSize);
                        if(!streetNames.ContainsKey(tileKey))
                        {                            
                            streetNames.Add(tileKey, new List<StreetName>());
                        }
                        streetNames[tileKey].Add(streetName);
                        UpdateStreetName(streetName);
                        parses++;
                        if (parses >= maxSpawnsPerFrame)
                        {
                            parses = 0;
                            yield return null;
                        }
                    }
                }
                yield return null;
            }
            previousPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            callback?.Invoke(tileChange);
        }

        private bool IsNamePresent(string name)
        {
            foreach(KeyValuePair<Vector2Int, List<string>> kv in uniqueNames)
                if(kv.Value.Contains(name))
                    return true;
            return false;
        }

        private void UpdateStreetName(StreetName streetName)
        {
            Vector3[] pathPoints = streetName.pathPoints;
            float splineLength = streetName.splineLength;
#if UNITY_EDITOR
            //debug spline
            Color rndColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
            for (int i = 1; i < pathPoints.Length; i++)
            {
                Vector3 height = Vector3.up * 10;
                Debug.DrawLine(pathPoints[i-1] + height, pathPoints[i] + height, rndColor);
            }
#endif
            //cache height so the optical raycaster can do its magic without overwriting the height
            float startHeight = streetName.text.gameObject.transform.position.y;
            (Vector3 startPos, Vector3 startForward) = SampleSplineAtDistance(pathPoints, 0.5f * splineLength);

            //do we need to flip text upside down relative to the camera forward angle
            float dot = Vector3.Dot(startForward, mainCamera.transform.right);
            float dotUp = Vector3.Dot(mainCamera.transform.forward, Vector3.up);
            bool reverse = dot > 0;
            bool isUp = dotUp > 0;

            streetName.text.gameObject.transform.position = new Vector3(startPos.x, startHeight, startPos.z);
            streetName.text.gameObject.transform.localScale = Vector3.one * streetName.textSize;

            var textInfo = streetName.text.textInfo;
            var mesh = textInfo.meshInfo[0].mesh;
            var vertices = streetName.vertices;
            var uvs = streetName.uvs;
            int vertexLength = textInfo.meshInfo[0].vertices.Length;
            int characterCount = textInfo.characterCount;
            TextMeshPro tmp = streetName.text;
            tmp.enabled = true;
            Transform tmpTransform = tmp.transform;
            Vector3 leftVertex = textInfo.characterInfo[0].bottomLeft;
            Vector3 rightVertex = textInfo.characterInfo[characterCount - 1].bottomRight;
            Vector3 worldLeft = tmpTransform.TransformPoint(leftVertex);
            Vector3 worldRight = tmpTransform.TransformPoint(rightVertex);
            float textLength = Vector3.Distance(worldLeft, worldRight);

            //0.005f is a factor to have a uniform distance value to increase the size of text when zoomed out
            float cameraScale = Vector3.Distance(mainCamera.transform.position, tmpTransform.gameObject.transform.position) * 0.005f;
            float scale = cameraScale;
            float characterDistance = textLength / characterCount;

            //compare if the textlength in worldspace is longer than the spline, if so we need to bring the scale down so it keeps fitting
            if (textLength * cameraScale > splineLength)
            {
                scale = splineLength / textLength;
            }
            float distToGround = Mathf.Abs(startHeight - mainCamera.transform.position.y) / 1000;
            if (scale > maxScale + distToGround)
                scale = maxScale + distToGround;
          
            for (int i = 0; i < characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[characterCount - 1 - i];
                if (!charInfo.isVisible) continue; //because of trailing or whitespaces messing up the calculations

                int vertexIndex = charInfo.vertexIndex;
                float charMidY = (charInfo.topLeft.y + charInfo.bottomLeft.y) * 0.5f;
                float charMidX = (charInfo.bottomLeft.x + charInfo.bottomRight.x) * 0.5f;

                //lets find the middle of the current character
                Vector3 charCenter = new Vector3(charMidX, charInfo.baseLine - streetName.baseLineY, 0f);
                Vector3 charWorldCenter = tmpTransform.TransformPoint(charCenter);
                Vector3 charMidXWorld = tmpTransform.TransformPoint(Vector3.right * charMidX);
                //lets calculate the distance from the left side to the character in world space
                float charDist = Vector3.Distance(charMidXWorld, worldLeft);
                float offset = reverse ? -0.5f * textLength + charDist : 0.5f * textLength - charDist;
                float dist = 0.5f * splineLength + offset * scale;
                //lets find the position on the spline relative to the center on the spline
                (Vector3 pos, Vector3 forward) = SampleSplineAtDistance(pathPoints, dist);
                Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
                //the orientation of the character quad
                Quaternion localRot = rot * Quaternion.Euler(Mathf.Sign(dotUp) * -90, Mathf.Sign(dot) * -90, 0);
                Vector3 localPos = pos - startPos;
                pos.y = startHeight;
                //calculate the positions of the 4 vertices per quad, we bring it to world space and apply matrix calculations
                for (int j = 0; j < 4; j++)
                {
                    Vector3 originalLocal = streetName.originalVertices[vertexIndex + j];
                    Vector3 vertexWorld = tmpTransform.TransformPoint(originalLocal);
                    Vector3 offsetFromCenter = vertexWorld - charWorldCenter;
                    offsetFromCenter *= scale;                    
                    Vector3 rotatedWorld = pos + localRot * offsetFromCenter;
                    vertices[vertexIndex + j] = tmpTransform.InverseTransformPoint(rotatedWorld); 
                    uvs[vertexIndex + j] = streetName.originalUvs[vertexIndex + j];
                }                
            }

            //we need to clear unused data from the buffer to prevent rendering artefacts
            for (int i = characterCount; i < vertexLength / 4; i++)
            {
                int vi = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    int idx = vi + j;
                    vertices[idx] = Vector3.zero;
                    uvs[idx] = Vector2.zero;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
            streetName.text.UpdateGeometry(mesh, 0);
        }

        public override void LayerToggled()
        {
            base.LayerToggled();
            if (isEnabled)
            {
                RestoreTiles();
            }
            else
                RemoveTiles();
        }

        private void Update()
        {
            UpdateStreetNames(false);
        }

        private Coroutine updateTilesRoutine = null;
        private bool visibleTilesDirty = false;
        private List<TileChange> queuedChanges = new List<TileChange>();
        private float lastUpdatedTimeStamp = 0;
        private float lastUpdatedInterval = 1f;
        private WaitForSeconds wfs = new WaitForSeconds(0.5f);


        public void RemoveTiles()
        {           
            foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
            {
                if (tile.Value == null || tile.Value.gameObject == null)
                    continue;

                if (tile.Value.runningCoroutine != null)
                {
                    StopCoroutine(tile.Value.runningCoroutine);
                    tile.Value.runningCoroutine = null;
                }
                RemoveStreetNames(tile.Value, tile.Key);
            }
        }

        private void RestoreTiles()
        {
            foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
            {
                if (tile.Value == null || tile.Value.gameObject == null)
                    continue;

                if (tile.Value.runningCoroutine != null)
                {
                    StopCoroutine(tile.Value.runningCoroutine);
                    tile.Value.runningCoroutine = null;
                }

                TileChange tileChange = new TileChange();
                tileChange.X = tile.Key.x;
                tileChange.Y = tile.Key.y;
                tiles[tile.Key].runningCoroutine = StartCoroutine(DownloadTextNameData(tileChange, tile.Value));
            }
        }

        protected virtual void OnPreUpdateTile(Tile tile)
        {

        }

        public void UpdateStreetNames(bool force)
        {
            //we changed height from zooming or yaw
            if (mainCamera.transform.position.y != previousPosition.y || mainCamera.transform.rotation.eulerAngles.y != previousRotation.y || force)
            {                
                previousPosition = mainCamera.transform.position;
                previousRotation = mainCamera.transform.rotation.eulerAngles;
                foreach (KeyValuePair<Vector2Int, List<StreetName>> kv in streetNames)
                {
                    List<StreetName> list = kv.Value;
                    if (!streetNamesUpdateQueue.ContainsKey(kv.Key))
                        streetNamesUpdateQueue.Add(kv.Key, new List<StreetName>());
                    streetNamesUpdateQueue[kv.Key].Clear();
                    streetNamesUpdateQueue[kv.Key].AddRange(list);
                }
            }

            //update in a queue to smooth out any spikes (large data will cause this)
            foreach (KeyValuePair<Vector2Int, List<StreetName>> kv in streetNamesUpdateQueue)
            {
                int cnt = 0;
                while (kv.Value.Count > 0)
                {
                    //by randomly updating this we smooth it out a bit
                    int rnd = Mathf.FloorToInt(UnityEngine.Random.value * kv.Value.Count);
                    StreetName name = kv.Value[rnd];
                    kv.Value.RemoveAt(rnd);                
                    UpdateStreetName(name);
                    cnt++;
                    if (cnt > maxSpawnsPerFrame)
                    {                        
                        break;
                    }
                }
            }
        }

        public static (Vector3 pos, Vector3 forward) SampleSplineAtDistance(Vector3[] points, float distance)
        {
            float traveled = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
                float segmentLength = Vector3.Distance(points[i], points[i + 1]);
                if (traveled + segmentLength >= distance)
                {
                    float t = (distance - traveled) / segmentLength;
                    Vector3 pos = Vector3.Lerp(points[i], points[i + 1], t);
                    Vector3 forward = (points[i + 1] - points[i]).normalized;
                    return (pos, forward);
                }
                traveled += segmentLength;
            }
            // fallback to end
            return (points[^1], (points[^1] - points[^2]).normalized);
        }
    }

    public class GeoJsonFeatureCollection
    {
        public string type;
        public List<GeoJsonFeature> features;
    }

    public class GeoJsonFeature
    {
        public string type;
        public Dictionary<string, object> properties;
        public Geometry geometry;
    }

    public class Geometry
    {
        public string type;
        public object coordinates;
    }
}
