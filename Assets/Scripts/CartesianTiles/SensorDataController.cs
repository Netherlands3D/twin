using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class SensorDataController : MonoBehaviour
    {
        public enum UrbanReleafPropertyType { None, Temperature, RelativeHumidity, ThermalDiscomfort }
        public UrbanReleafPropertyType SensorPropertyType;
        public float Maximum;
        public float Minimum;        
        public Color MaxColor;
        public Color MinColor;

        public List<UrbanReleafCell> Cells { get { return urbanReleafCells; } }
          
        private List<UrbanReleafCell> urbanReleafCells = new List<UrbanReleafCell>();

        //url keys
        private const string areaKey = "&observation_area=";
        private const string seperate = "%2C";
        private const string timeStartKey = "&observation_datetime_start=";
        private const string timeEndKey = "&observation_datetime_end=";
        private const string observationLimitKey = "&observations_limit=";
        private const string timeFormatSpecifier = "s";
        
        private float edgeMultiplier = 1.15f; //lets add 15% to the edges of the polygon to cover the seems between tiles
        private int observationLimit = 1000; //the maximum data points per tile retrieved. a low number sometimes causes cells not to properly overlap with other tiles
        private int timeWindowSeconds = 3600 * 24 * 365 * 10; //for some reason sensors are not updated recently

        public struct UrbanReleafCell
        {
            public float value;
            public float lat;
            public float lon;
            public float quality;
            public UrbanReleafPropertyType type;
            public Vector3 unityPosition;
        }

        public double[] GetLongLatFromPosition(Vector3 position, Tile tile)
        {
            var unityCoordinate = new Coordinate(
                CoordinateSystem.RD,
                position.x + tile.tileKey.x + 0.5f * tile.layer.tileSize,
                position.z + tile.tileKey.y + 0.5f * tile.layer.tileSize,
                0
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
            return new double[2] { coord.Points[1], coord.Points[0] };
        }       
        
        //TODO optimize with stringbuilder
        public string GeneratePolygonUrlForTile(Tile tile)
        {
            //make square polygon
            double[][] coords = new double[4][];
            int tileSize = tile.layer.tileSize;
            coords[0] = GetLongLatFromPosition(new Vector3(-tileSize * 0.5f * edgeMultiplier, 0, tileSize * 0.5f * edgeMultiplier), tile);
            coords[1] = GetLongLatFromPosition(new Vector3(-tileSize * 0.5f * edgeMultiplier, 0, -tileSize * 0.5f * edgeMultiplier), tile);
            coords[2] = GetLongLatFromPosition(new Vector3(tileSize * 0.5f * edgeMultiplier, 0, -tileSize * 0.5f * edgeMultiplier), tile);
            coords[3] = GetLongLatFromPosition(new Vector3(tileSize * 0.5f * edgeMultiplier, 0, tileSize * 0.5f * edgeMultiplier), tile);

            //debug
            //tile.gameObject.GetComponent<TileSensorData>().SetCornerCoordsTest( coords );                        
            
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
       
        public void ProcessDataFromJson(string json)
        {
            //could be refactored to work with a recursive method and store a tree but this will store less memory
            urbanReleafCells.Clear();
            JArray jsonArray = JArray.Parse(json);
            List<JObject> jsonObjects = jsonArray.OfType<JObject>().ToList();
            foreach (JObject j in jsonObjects)
            {
                float value = 0;
                float lat = 0;
                float lon = 0;
                float quality = 0;
                UrbanReleafPropertyType propertyType = UrbanReleafPropertyType.None;
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
                            if (gKv.Key == "quality")
                            {
                                quality = float.Parse(gKv.Value.ToString());
                            }
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
                                string[] names = Enum.GetNames(typeof(UrbanReleafPropertyType));
                                for (int i = 0; i < names.Length; i++)
                                    if (names[i].Contains(name))
                                    {
                                        propertyType = (UrbanReleafPropertyType)i;
                                    }
                            }
                        }
                    }
                }

                if (propertyType == SensorPropertyType)
                {
                    UrbanReleafCell cell = new UrbanReleafCell();
                    cell.value = value;
                    cell.lon = lon;
                    cell.lat = lat;
                    cell.quality = quality;
                    cell.type = propertyType;

                    double[] lonlat = new double[2];
                    lonlat[0] = cell.lon;
                    lonlat[1] = cell.lat;
                    Vector3 unityPosition = GetProjectedPositionFromLonLat(lonlat, 0);
                    cell.unityPosition = unityPosition;
                    urbanReleafCells.Add(cell);
                }
            }
        }       

        public void ClearCells()
        {
            urbanReleafCells.Clear();
        }

        //TODO make this more oop
        public static Vector3 GetProjectedPositionFromLonLat(double[] coordinate, float height)
        {
            var unityCoordinate = new Coordinate(
                CoordinateSystem.WGS84,
                coordinate[1],
                coordinate[0],
                0
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.Unity);
            Vector3 position = coord.ToUnity();
            position.y = height;
            return position;
        }
    }
}
