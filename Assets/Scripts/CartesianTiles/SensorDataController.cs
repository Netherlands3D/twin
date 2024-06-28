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

        public void ProcessDataFromJson(string json)
        {
            List<UrbanReleafCell> urbanReleafCells = new List<UrbanReleafCell>();
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
    }
}
