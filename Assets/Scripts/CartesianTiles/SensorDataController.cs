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
        public Texture2D DataTexture { get { return dataTexture; } }
        private enum UrbanReleafPropertyType { None, Temperature, RelativeHumidity, ThermalDiscomfort }

        [SerializeField]
        private Texture2D dataTexture;

        //TODO should this be cleared because of memory limitations?
        private List<UrbanReleafCell> urbanReleafCells = new List<UrbanReleafCell>();

        //values
        private const string areaKey = "&observation_area=";
        private const string seperate = "%2C";

        private struct UrbanReleafCell
        {
            public float value;
            public float lat;
            public float lon;
            public float quality;
            public UrbanReleafPropertyType type;
        }

        public void SetTexture(Texture2D texture)
        {
            dataTexture = texture;
        }

        public List<double[]> GetAllLongLatPositions()
        {
            List<double[]> result = new List<double[]>();
            foreach(var cell in urbanReleafCells)
                result.Add(new double[2] { cell.lon, cell.lat });
            return result;
        }

        //for testing only
        public void ProjectAllSensorPositions()
        {
            List<double[]> positions = GetAllLongLatPositions();
            foreach (double[] position in positions)
            {
                Vector3 unityPosition = GetProjectedPositionFromLongLat(position, ImageProjectionLayer.ProjectorHeight);
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
            coords[0] = GetLongLatFromPosition(tile.gameObject.transform.position + new Vector3(-tileSize * 0.5f, 0, tileSize * 0.5f));
            coords[1] = GetLongLatFromPosition(tile.gameObject.transform.position + new Vector3(-tileSize * 0.5f, 0, -tileSize * 0.5f));
            coords[2] = GetLongLatFromPosition(tile.gameObject.transform.position + new Vector3(tileSize * 0.5f, 0, -tileSize * 0.5f));
            coords[3] = GetLongLatFromPosition(tile.gameObject.transform.position + new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f));
            
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

        public void UpdateTexture()
        {
            //TODO: CREATE TEXTURE FROM DATA
            //form hex grid based on LOD
            //draw pixel set based on precalculated math
            //setpixels32
            //texture apply
            //interval 30 sec update
            //callbacks?
        }

        private double[] GetLongLatFromPosition(Vector3 position)
        {
            var unityCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                position.x,
                position.y,
                position.z
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
            return new double[2] { coord.Points[1], coord.Points[0] };
        }

        private Vector3 GetProjectedPositionFromLongLat(double[] coordinate, float height)
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
