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

namespace Netherlands3D.Functionalities.Toponyms
{
    public class GeoJSONStreetLayerGameObject : GeoJSONTextLayer
    {
        private string geoJsonUrlGeometry = "https://service.pdok.nl/rws/nwbwegen/wfs/v1_0?service=WFS&version=2.0.0&request=GetFeature&typeNames=nwbwegen:wegvakken&srsName=EPSG:4326&outputFormat=application/json&bbox=";

        private static Dictionary<string, bool> uniqueNames = new Dictionary<string, bool>();
        private static Dictionary<Vector3, Vector3> forwardVectors = new Dictionary<Vector3, Vector3>();


        protected override IEnumerator DownloadTextNameData(TileChange tileChange, Tile tile, System.Action<TileChange> callback = null)
        {          
            string geomUrl = $"{geoJsonUrlGeometry}{tileChange.X},{tileChange.Y},{(tileChange.X + tileSize)},{(tileChange.Y + tileSize)}";

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
                        // Pak de binnenste array van coordinaten
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

                    float splineLength = 0f;
                    Vector3[] pathPoints = new Vector3[positions.Count];
                    Color rndColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
                    for (int i = 0; i < positions.Count; i++)
                    {
                        Vector3 c = positions[i].ToUnity();
                        c.y = 0;
                        pathPoints[i] = c;
                        if (i > 0)
                        {
                            float len = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
                            splineLength += len;
                        }
                        //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //go.transform.position = new Vector3(c.x, 100, c.z);
                        //go.GetComponent<MeshRenderer>().material.color = rndColor;
                    }


                    foreach (TextsAndSize textAndSize in textsAndSizes)
                    {

                        var textObject = Instantiate(textPrefab);
                        textObject.name = naam;
                        textObject.transform.SetParent(tile.gameObject.transform, true);
                        textObject.GetComponent<TextMeshPro>().text = naam;

                        //Turn the text object so it faces up
                        //textObject.transform.Rotate(Vector3.left, -90, Space.Self);

                        (Vector3 startPos, Vector3 startForward) = SampleSplineAtDistance(pathPoints, 0.5f * splineLength);

                        textObject.transform.position = startPos;
                        textObject.transform.localScale = Vector3.one * textAndSize.drawWithSize;

                        TextMeshPro tmp = textObject.GetComponent<TextMeshPro>();

                        tmp.ForceMeshUpdate();
                        var textInfo = tmp.textInfo;
                        var mesh = textInfo.meshInfo[0].mesh;
                        var vertices = mesh.vertices;

                        Vector3 leftVertex = textInfo.characterInfo[0].bottomLeft;
                        Vector3 rightVertex = textInfo.characterInfo[textInfo.characterCount - 1].bottomRight;
                        Vector3 worldLeft = tmp.transform.TransformPoint(leftVertex);
                        Vector3 worldRight = tmp.transform.TransformPoint(rightVertex);
                        float textLength = Vector3.Distance(worldLeft, worldRight);
                        float scale = 1;
                        float characterDistance = textLength / textInfo.characterCount;
                        if (textLength > splineLength)
                        {
                            scale = splineLength / textLength;
                        }

                        for (int i = 0; i < textInfo.characterCount; i++)
                        {
                            var charInfo = textInfo.characterInfo[textInfo.characterCount - 1 - i];
                            if (!charInfo.isVisible) continue; // <- very important

                            int vertexIndex = charInfo.vertexIndex;
                            Vector3 center = (vertices[vertexIndex] + vertices[vertexIndex + 2]) * 0.5f;
                            Vector3 worldCenter = tmp.transform.TransformPoint(center) - tmp.transform.position;
                            float dist = 0.5f * splineLength + (-0.5f * textLength + i * characterDistance) * scale;

                            (Vector3 pos, Vector3 forward) = SampleSplineAtDistance(pathPoints, dist);


                            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            go.transform.position = new Vector3(pos.x, 100, pos.z);
                            go.GetComponent<MeshRenderer>().material.color = rndColor;
                            Vector3 localPos = pos - startPos;
                            for (int j = 0; j < 4; j++)
                            {
                                Vector3 originalLocal = vertices[vertexIndex + j];
                                Vector3 vertexWorld = tmp.transform.TransformPoint(originalLocal);
                                Vector3 offsetFromCenter = vertexWorld - tmp.transform.TransformPoint(center);
                                offsetFromCenter *= scale;
                                Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
                                rot *= Quaternion.Euler(90, 90, 0);
                                Vector3 rotatedWorld = pos + rot * offsetFromCenter;
                                //forwardVectors.Add(rotatedWorld, Vector3.up * 10);
                                vertices[vertexIndex + j] = tmp.transform.InverseTransformPoint(rotatedWorld);
                            }
                        }

                        mesh.vertices = vertices;
                        mesh.RecalculateBounds();
                        tmp.UpdateGeometry(mesh, 0);
                    }
                }
                yield return null;
            }
            callback?.Invoke(tileChange);
        }

        //private void OnDrawGizmos()
        //{
        //    foreach (KeyValuePair<Vector3, Vector3> kv in forwardVectors)
        //    {
        //        Debug.DrawLine(kv.Key, kv.Key + kv.Value, Color.green);
        //    }
        //}

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
