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

        public List<UrbanReleafCell> Cells { get { return urbanReleafCells; } }

        public enum UrbanReleafPropertyType { None, Temperature, RelativeHumidity, ThermalDiscomfort }
        
        //TODO should this be cleared because of memory limitations?
        private List<UrbanReleafCell> urbanReleafCells = new List<UrbanReleafCell>();

        //values
        private const string areaKey = "&observation_area=";
        private const string seperate = "%2C";
        

        public struct UrbanReleafCell
        {
            public float value;
            public float lat;
            public float lon;
            public float quality;
            public UrbanReleafPropertyType type;
        }
       
        public List<double[]> GetAllLonLatPositions()
        {
            List<double[]> result = new List<double[]>();
            foreach(var cell in urbanReleafCells)
                result.Add(new double[2] { cell.lon, cell.lat });
            return result;
        }

        public double[] GetLongLatFromPosition(Vector3 position, Tile tile)
        {
            var unityCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                position.x + tile.gameObject.transform.position.x,
                position.y + tile.gameObject.transform.position.y,
                position.z + tile.gameObject.transform.position.z
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
            return new double[2] { coord.Points[1], coord.Points[0] };
        }

        //for testing only
        public void ProjectAllSensorPositions()
        {
            List<double[]> positions = GetAllLonLatPositions();
            foreach (double[] position in positions)
            {
                Vector3 unityPosition = GetProjectedPositionFromLonLat(position, ImageProjectionLayer.ProjectorHeight);
                GameObject test = GameObject.CreatePrimitive(PrimitiveType.Cube);
                test.transform.position = unityPosition;
                test.transform.localScale = Vector3.one * 50;
            }
        }
        
        //TODO optimize with stringbuilder
        public string GeneratePolygonUrlForTile(Tile tile)
        {
            //make square polygon
            double[][] coords = new double[4][];
            int tileSize = tile.layer.tileSize;
            coords[0] = GetLongLatFromPosition(new Vector3(-tileSize * 0.5f, 0, tileSize * 0.5f), tile);
            coords[1] = GetLongLatFromPosition(new Vector3(-tileSize * 0.5f, 0, -tileSize * 0.5f), tile);
            coords[2] = GetLongLatFromPosition(new Vector3(tileSize * 0.5f, 0, -tileSize * 0.5f), tile);
            coords[3] = GetLongLatFromPosition(new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f), tile);
            
            string polygonUrl = string.Empty;
            for (int i = 0; i < coords.Length; i++)
                polygonUrl += areaKey + coords[i][0].ToString() + seperate + coords[i][1].ToString();

            //add first vertex to complete the polygon
            polygonUrl += areaKey + coords[0][0].ToString() + seperate + coords[0][1].ToString();
            return polygonUrl;
        }

        public void ProcessDataFromJson(string json)
        {
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

                UrbanReleafCell cell = new UrbanReleafCell();
                cell.value = value;
                cell.lon = lon;
                cell.lat = lat;
                cell.quality = quality;
                cell.type = propertyType;
                urbanReleafCells.Add(cell);
            }
        }       

        public Vector3 GetProjectedPositionFromLonLat(double[] coordinate, float height)
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
