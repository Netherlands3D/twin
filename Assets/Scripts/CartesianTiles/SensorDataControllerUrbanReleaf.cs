using Netherlands3D.CartesianTiles;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public class SensorDataControllerUrbanReleaf : SensorDataController
    {
        //url keys
        private const string areaKey = "&observation_area=";
        private const string seperate = "%2C";
        private const string timeStartKey = "&observation_datetime_start=";
        private const string timeEndKey = "&observation_datetime_end=";
        private const string observationLimitKey = "&observations_limit=";
        private const string timeFormatSpecifier = "s";

        private int observationLimit = 1000; //the maximum data points per tile retrieved. a low number sometimes causes cells not to properly overlap with other tiles
        private int timeWindowSeconds = 3600 * 24 * 365 * 10; //for some reason sensors are not updated recently

        public override UnityWebRequest GetRequest(Tile tile, string baseUrl)
        {
            string polygonUrl = GeneratePolygonUrlForTile(tile);
            string url = baseUrl + polygonUrl;
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            return webRequest;
        }

        //TODO optimize with stringbuilder
        public override string GeneratePolygonUrlForTile(Tile tile)
        {
            //make square polygon
            double[][] coords = GetLongLatCornersFromTile(tile);
            string polygonUrl = string.Empty;

            //get the observationlimit
            string observationUrl = observationLimitKey + observationLimit.ToString();
            polygonUrl += observationUrl;

            //add each vertex for the polygon
            for (int i = 0; i < coords.Length; i++)
                polygonUrl += areaKey + coords[i][0].ToString() + seperate + coords[i][1].ToString();

            //add first vertex to complete the polygon
            polygonUrl += areaKey + coords[0][0].ToString() + seperate + coords[0][1].ToString();

            string timeUrl = GetTimeUrl();
            return polygonUrl + timeUrl;
        }

        public string GetTimeUrl()
        {
            //get the observation start time and end time and create a timewindow url            
            DateTime end = DateTime.Now;
            string sortableEndTime = end.ToString(timeFormatSpecifier);
            DateTime start = end.AddSeconds(-timeWindowSeconds);
            string sortableStartTime = start.ToString(timeFormatSpecifier);
            string timeUrl = timeStartKey + sortableStartTime + timeEndKey + sortableEndTime;
            return timeUrl;
        }

        public override List<SensorCell> GetSensorCellsForTile(Tile tile)
        {
            //the results are already filtered so return the cells
            return Cells;
        }

        public override void ProcessDataFromJson(string json)
        {
            //could be refactored to work with a recursive method and store a tree but this will store less memory
            base.ProcessDataFromJson(json);
            JArray jsonArray = JArray.Parse(json);
            List<JObject> jsonObjects = jsonArray.OfType<JObject>().ToList();
            foreach (JObject j in jsonObjects)
            {
                float value = 0;
                float lat = 0;
                float lon = 0;
                //float quality = 0;
                SensorPropertyType sensorType = SensorPropertyType.None;
                Dictionary<string, object> values = JObject.FromObject(j).ToObject<Dictionary<string, object>>();
                foreach (KeyValuePair<string, object> kv in values)
                {
                    if (kv.Key == "value")
                        value = float.Parse(kv.Value.ToString());
                    if (kv.Key == "geolocation")
                    {
                        Dictionary<string, object> geoKv = JObject.FromObject(kv.Value).ToObject<Dictionary<string, object>>();
                        foreach (KeyValuePair<string, object> gKv in geoKv)
                        {
                            if (gKv.Key == "coordinates")
                            {
                                Dictionary<string, object> coordKv = JObject.FromObject(gKv.Value).ToObject<Dictionary<string, object>>();
                                foreach (KeyValuePair<string, object> coord in coordKv)
                                {
                                    if (coord.Key == "latitude")
                                    {
                                        lat = float.Parse(coord.Value.ToString());
                                    }
                                    if (coord.Key == "longitude")
                                    {
                                        lon = float.Parse(coord.Value.ToString());
                                    }
                                }
                            }
                            //if (gKv.Key == "quality")
                            //{
                            //    quality = float.Parse(gKv.Value.ToString());
                            //}
                        }
                    }
                    if (kv.Key == "observed_property")
                    {
                        Dictionary<string, object> propertyKv = JObject.FromObject(kv.Value).ToObject<Dictionary<string, object>>();
                        foreach (KeyValuePair<string, object> pkv in propertyKv)
                        {
                            if (pkv.Key == "name")
                            {
                                string name = Regex.Replace(pkv.Value.ToString(), @"\s", string.Empty);
                                string[] names = Enum.GetNames(typeof(SensorPropertyType));
                                for (int i = 0; i < names.Length; i++)
                                    if (names[i].Contains(name))
                                    {
                                        sensorType = (SensorPropertyType)i;
                                    }
                            }
                        }
                    }
                }

                if (sensorType == propertyType)
                {
                    SensorCell cell = new SensorCell();
                    cell.value = value;
                    cell.lon = lon;
                    cell.lat = lat;
                    //cell.quality = quality;
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
