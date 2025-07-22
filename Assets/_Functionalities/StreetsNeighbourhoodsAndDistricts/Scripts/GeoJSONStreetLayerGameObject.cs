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
using Netherlands3D.Twin.FloatingOrigin;
using System.Linq;

namespace Netherlands3D.Functionalities.Toponyms
{
    public class GeoJSONStreetLayerGameObject : GeoJSONTextLayer
    {
        private string geoJsonUrlGeometry = "https://service.pdok.nl/rws/nwbwegen/wfs/v1_0?service=WFS&version=2.0.0&request=GetFeature&typeNames=nwbwegen:wegvakken&srsName=EPSG:4326&outputFormat=application/json&bbox=";
        private static Dictionary<string, bool> uniqueNames = new Dictionary<string, bool>();
        private static Dictionary<Vector2Int, List<StreetName>> streetNames = new Dictionary<Vector2Int, List<StreetName>>();
        private Material textMaterial;
        //private Coordinate previousCoordinate;       
        private Vector3 previousPosition;
        private Vector3 previousRotation;
        private Camera mainCamera;

        public struct StreetName
        {
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
                for(int i = 0; i < uv0array.Length; i++)
                    originalUvs[i] = uv0array[i];

                //originalNormals = meshInfo.normals;
                //originalTangents = meshInfo.tangents;
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
            }            
            public string name;
            public float textSize;
            public float splineLength;
            public TextMeshPro text;
            public Coordinate[] positions;
            public Vector3[] pathPoints; 
            public Vector3[] vertices;
            public Vector2[] uvs;

            public Vector3[] originalVertices;
            public Vector2[] originalUvs;
            //public Vector3[] originalNormals;
            //public Vector4[] originalTangents;


        }

        public override void Start()
        {
            base.Start();
            textMaterial = textPrefab.GetComponent<MeshRenderer>().sharedMaterial;
            //this solves the problem for blurry characters at greater distances but is probably not the right way to handle this
          

            mainCamera = Camera.main;
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
                MeshFilter mf = tile.gameObject.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    Destroy(tile.gameObject.GetComponent<MeshFilter>().sharedMesh);
                }
                if(streetNames.ContainsKey(tileKey))
                    foreach(StreetName streetName in streetNames[tileKey])
                        Destroy(streetName.text);
                streetNames.Remove(tileKey);

                Destroy(tiles[tileKey].gameObject);
            }
        }
       

        protected override IEnumerator DownloadTextNameData(TileChange tileChange, Tile tile, System.Action<TileChange> callback = null)
        {          
            string geomUrl = $"{geoJsonUrlGeometry}{tileChange.X},{tileChange.Y},{(tileChange.X + tileSize)},{(tileChange.Y + tileSize)}";
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
            var streetnameRequest = UnityWebRequest.Get(geomUrl);
            tile.runningWebRequest = streetnameRequest;
            yield return streetnameRequest.SendWebRequest();

            if (streetnameRequest.result == UnityWebRequest.Result.Success)
            {
                uniqueNames.Clear();
                GeoJsonFeatureCollection featureCollection = JsonConvert.DeserializeObject<GeoJsonFeatureCollection>(streetnameRequest.downloadHandler.text);
                foreach (var feature in featureCollection.features)
                {
                    string naam = feature.properties["sttNaam"].ToString();

                    if (uniqueNames.ContainsKey(naam))
                        continue;

                    uniqueNames.Add(naam, true);
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
                        textObject.name = naam;
                        textObject.transform.SetParent(tile.gameObject.transform, true);
                        TextMeshPro tmp = textObject.GetComponent<TextMeshPro>();
                        StreetName streetName = new StreetName(naam, tmp, latLonPositions, textAndSize.drawWithSize);
                        if(!streetNames.ContainsKey(tileKey))
                        {                            
                            streetNames.Add(tileKey, new List<StreetName>());
                        }
                        streetNames[tileKey].Add(streetName);
                        UpdateStreetName(streetName);
                    }
                }
                yield return null;
            }
            previousPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            callback?.Invoke(tileChange);
        }


        private void UpdateStreetName(StreetName streetName)
        {
            Vector3[] pathPoints = streetName.pathPoints;
            float splineLength = streetName.splineLength;
            //debug spline
            Color rndColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
            for (int i = 1; i < pathPoints.Length; i++)
            {
                Vector3 height = Vector3.up * 10;
                Debug.DrawLine(pathPoints[i-1] + height, pathPoints[i] + height, rndColor);
            }
            //cache height so the optical raycaster can do its magic without overwriting the height
            float startHeight = streetName.text.gameObject.transform.position.y;
            (Vector3 startPos, Vector3 startForward) = SampleSplineAtDistance(pathPoints, 0.5f * splineLength);

            //do we need to flip text upside down relative to the camera forward angle
            float dot = Vector3.Dot(startForward, mainCamera.transform.right);
            bool reverse = dot > 0;

            streetName.text.gameObject.transform.position = new Vector3(startPos.x, startHeight, startPos.z);
            streetName.text.gameObject.transform.localScale = Vector3.one * streetName.textSize;

            var textInfo = streetName.text.textInfo;
            var mesh = textInfo.meshInfo[0].mesh;
            var vertices = streetName.vertices;
            var uvs = streetName.uvs;
            //var normals = mesh.normals;
            //var tangents = mesh.tangents;

            TextMeshPro tmp = streetName.text;
            tmp.enabled = true;
            Transform tmpTransform = tmp.transform;

            Vector3 leftVertex = textInfo.characterInfo[0].bottomLeft;
            Vector3 rightVertex = textInfo.characterInfo[textInfo.characterCount - 1].bottomRight;
            Vector3 worldLeft = tmpTransform.TransformPoint(leftVertex);
            Vector3 worldRight = tmpTransform.TransformPoint(rightVertex);
            float textLength = Vector3.Distance(worldLeft, worldRight);
            float cameraScale = Vector3.Distance(mainCamera.transform.position, tmpTransform.gameObject.transform.position) * 0.005f;
            float scale = cameraScale;
            float characterDistance = textLength / textInfo.characterCount;

            //compare if the textlength in worldspace is longer than the spline, if so we need to bring the scale down so it keeps fitting
            if (textLength * cameraScale > splineLength)
            {
                scale = splineLength / textLength;
            }
            if (scale > 1)
                scale = 1;

            int characterCount = textInfo.characterCount;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float baseLineY = float.MaxValue;
            for (int i = 0; i < characterCount; i++)
            {
                var ci = textInfo.characterInfo[i];
                if (!ci.isVisible) continue;

                minY = Mathf.Min(minY, ci.bottomLeft.y);
                maxY = Mathf.Max(maxY, ci.topLeft.y);
                baseLineY = Mathf.Min(baseLineY, ci.baseLine);
            }

            float centerY = (minY + maxY) * 0.5f;
            for (int i = 0; i < characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[characterCount - 1 - i];
                if (!charInfo.isVisible) continue; //because of trailing or whitespaces messing up the calculations

                int vertexIndex = charInfo.vertexIndex;
                float charMidY = (charInfo.topLeft.y + charInfo.bottomLeft.y) * 0.5f;
                float charMidX = (charInfo.bottomLeft.x + charInfo.bottomRight.x) * 0.5f;

                Vector3 charCenter = new Vector3(charMidX, charInfo.baseLine - baseLineY, 0f);
                Vector3 worldCenter = tmpTransform.TransformPoint(charCenter) - tmpTransform.position;  
                Vector3 charMidXWorld = tmpTransform.TransformPoint(Vector3.right * charMidX);
                float charDist = Vector3.Distance(charMidXWorld, worldLeft);
                float offset = reverse ? -0.5f * textLength + charDist : 0.5f * textLength - charDist;
                float dist = 0.5f * splineLength + offset * scale;
                (Vector3 pos, Vector3 forward) = SampleSplineAtDistance(pathPoints, dist);
                Vector3 localPos = pos - startPos;
                pos.y = startHeight;
                for (int j = 0; j < 4; j++)
                {
                    Vector3 originalLocal = streetName.originalVertices[vertexIndex + j];
                    Vector3 vertexWorld = tmpTransform.TransformPoint(originalLocal);
                    Vector3 offsetFromCenter = vertexWorld - tmpTransform.TransformPoint(charCenter);
                    offsetFromCenter *= scale;

                    Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
                    rot *= Quaternion.Euler(90, Mathf.Sign(dot) * -90, 0);
                    Vector3 rotatedWorld = pos + rot * offsetFromCenter;
                    vertices[vertexIndex + j] = tmpTransform.InverseTransformPoint(rotatedWorld); 
                    uvs[vertexIndex + j] = streetName.originalUvs[vertexIndex + j];
                }                
            }

            //we need to clear unused data from the buffer to prevent rendering artefacts
            for (int i = characterCount; i < textInfo.meshInfo[0].vertices.Length / 4; i++)
            {
                int vi = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    int idx = vi + j;
                    vertices[idx] = Vector3.zero;
                    uvs[idx] = Vector2.zero;
                    //normals[idx] = Vector3.back;
                    //tangents[idx] = Vector4.zero;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            //mesh.normals = normals;
            //mesh.tangents = tangents;
            mesh.RecalculateBounds();
            streetName.text.UpdateGeometry(mesh, 0);
        }

        private void Update()
        {           
            //we changed height from zooming or yaw
            if(mainCamera.transform.position.y != previousPosition.y || mainCamera.transform.rotation.eulerAngles.y != previousRotation.y)
            {
                previousPosition = mainCamera.transform.position;
                previousRotation = mainCamera.transform.rotation.eulerAngles;
                foreach (KeyValuePair<Vector2Int, List<StreetName>> kv in streetNames)
                {
                    List<StreetName> list = kv.Value;
                    foreach (StreetName streetName in list)
                        UpdateStreetName(streetName);
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
