using Netherlands3D.CartesianTiles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class SensorDataControllerPientereTuinen : SensorDataController
    {        
        [SerializeField]
        private KeyVault keyVault;

        public override UnityWebRequest GetRequest(Tile tile, string baseUrl)
        {   
            StoredAuthorization auth = keyVault.GetStoredAuthorization(baseUrl);
            string apiKey = auth.key;
            //TODO how to query the spatial data!?
            //string polygonUrl = GeneratePolygonUrlForTile(tile);
            string url = baseUrl + "?page=0&size=1000";// + polygonUrl;
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            webRequest.SetRequestHeader("accept", "application/json");
            webRequest.SetRequestHeader("wecity-api-key", apiKey);
            return webRequest;
        }

        public override List<SensorCell> GetSensorCellsForTile(Tile tile)
        {
            List<SensorCell> cells = Cells;
            List<SensorCell> results = new List<SensorCell>();
            double[][] coords = GetLongLatCornersFromTile(tile);
            foreach(SensorCell cell in cells) 
            {
                //TODO filter the cells within tile bounds
            }
            return Cells;
        }

        public override void ProcessDataFromJson(string json)
        {
            //in case of static cell data, meaning there is no way to spatial query the data, we will need to keep static cell data to query from that
            if (StaticSensorData && staticCells.Count > 0)
                return;

            base.ProcessDataFromJson(json);
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            Dictionary<string, object> embedded = JObject.FromObject(data.Values.First()).ToObject<Dictionary<string, object>>();
            foreach(KeyValuePair<string, object> kvp in embedded)
            {
                JArray measurements = (JArray)kvp.Value;
                foreach (JObject node in measurements)
                {
                    bool hasError = false;
                    float value = 0;
                    float lat = 0;
                    float lon = 0;
                    //float quality = 0;
                    SensorPropertyType sensorType = propertyType;
                    Dictionary<string, object> values = node.ToObject<Dictionary<string, object>>();
                    foreach(KeyValuePair<string, object> kv in values)
                    {
                        if (kv.Key == "latitude")
                        {
                            if(kv.Value == null)
                            {
                                hasError = true;
                                continue;
                            }
                            lat = float.Parse(kv.Value.ToString());
                        }
                        if (kv.Key == "longitude")
                        {
                            if (kv.Value == null)
                            {
                                hasError = true;
                                continue;
                            }
                            lon = float.Parse(kv.Value.ToString());
                        }
                        if(propertyType == SensorPropertyType.Temperature && kv.Key == "temperatureCelsius")
                        {
                            if (kv.Value == null)
                            {
                                hasError = true;
                                continue;
                            }
                            value = float.Parse(kv.Value.ToString());
                        }
                        if(propertyType == SensorPropertyType.RelativeHumidity && kv.Key == "moisturePercentage")
                        {
                            if (kv.Value == null)
                            {
                                hasError = true;
                                continue;
                            }
                            value = float.Parse(kv.Value.ToString()) * 100; //* 100 because the scale used is a value ranging from 0 to 1 instead of 0 to 100
                        }
                    }
                    if (!hasError)
                    {
                        SensorCell cell = new SensorCell();
                        cell.value = value;
                        cell.lon = lon;
                        cell.lat = lat;
                        cell.type = sensorType;

                        double[] lonlat = new double[2];
                        lonlat[0] = cell.lon;
                        lonlat[1] = cell.lat;
                        Vector3 unityPosition = GetProjectedPositionFromLonLat(lonlat, 0);
                        cell.unityPosition = unityPosition;
                        AddCell(cell);
                    }
                }
            }
        }
    }
}
