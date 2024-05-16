using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.Twin.UI.LayerInspector;
using Newtonsoft.Json;
using UnityEngine;
using GeoJSON.Net;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Feature;
using SimpleJSON;

namespace Netherlands3D.Twin
{
    // public enum GeoJSONType
    // {
    //     Geometry, 
    //     Feature,
    //     FeatureCollection
    // }

    public class GeoJSONLayer : LayerNL3DBase
    {
        public GeoJSONObjectType Type { get; private set; }
        public CRSBase CRS { get; private set; }

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            throw new System.NotImplementedException();
        }

        public void ParseGeoJSON(string filePath)
        {
            StartCoroutine(ParseGeoJSON(filePath, 1000));
        }

        private IEnumerator ParseGeoJSON(string filePath, int maxParsesPerFrame = Int32.MaxValue)
        {
            var reader = new StreamReader(filePath);
            print("start geoJSON parse");

            var jsonReader = new JsonTextReader(reader);

            JsonSerializer serializer = new JsonSerializer();
            FindTypeAndCRS(jsonReader, serializer);

            //reset position of reader
            jsonReader.Close();
            reader = new StreamReader(filePath);
            jsonReader = new JsonTextReader(reader);

            // int tokenCounter = 0;

            while (jsonReader.Read())
            {
                // tokenCounter++;
                // if ((tokenCounter % maxParsesPerFrame) == 0) yield return null;

                // if (jsonReader.Value != null)
                // {
                //     Debug.Log(tokenCounter + " Token: " + jsonReader.TokenType + " Value: " + jsonReader.Value);
                // }
                // else
                // {
                //     Debug.Log(tokenCounter + " Cannot read Token: " + jsonReader.TokenType);
                // }

                //read features depending on type
                if (jsonReader.TokenType == JsonToken.PropertyName && IsAtFeaturesToken(jsonReader))
                {
                    jsonReader.Read(); //start array
                    // tokenCounter++;

                    print("features token found");

                    yield return ReadFeaturesArray(jsonReader, serializer);
                }
            }

            jsonReader.Close();
        }

        private static IEnumerator ReadFeaturesArray(JsonTextReader jsonReader, JsonSerializer serializer)
        {
            var features = new List<Feature>();
            var startTime = Time.realtimeSinceStartup;
            
            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.EndArray)
                {
                    // end of feature array, stop parsing here
                    break;
                }

                var feature = serializer.Deserialize<Feature>(jsonReader);
                features.Add(feature);
                
                var parseDuration = Time.realtimeSinceStartup - startTime;
                if (parseDuration > 0.01f)
                {
                    // print(parseDuration + " time exceeded. waiting a frame " + features.Count + " features parsed");
                    yield return null;
                    startTime = Time.realtimeSinceStartup;
                }
            }
        }

        private void FindTypeAndCRS(JsonTextReader reader, JsonSerializer serializer)
        {
            //reader must be at 0 for this to work properly
            while (reader.TokenType != JsonToken.PropertyName)
            {
                reader.Read(); // read until property name is found (highest depth in json hierarchy)
            }

            bool typeFound = false;
            bool crsFound = false;
            do //process the found object, and continue reading after processing is done
            {
                if (!IsAtTypeToken(reader) && !IsAtCRSToken(reader))
                {
                    reader.Skip(); //if the found token is is not "type" or "crs", skip this object
                }

                //read type
                if (IsAtTypeToken(reader))
                {
                    ReadType(reader, serializer);
                    typeFound = true;
                }

                //read crs
                if (IsAtCRSToken(reader))
                {
                    ReadCRS(reader, serializer);
                    crsFound = true;
                }

                if (typeFound && crsFound)
                    return;
            } while (reader.Read());
        }

        private void ReadCRS(JsonTextReader reader, JsonSerializer serializer)
        {
            //Default if no CRS object is specified
            CRS = DefaultCRS.Instance;
            reader.Read(); // go to start of CRS object

            //we need to stay within our CRS object, because there can also be "type" and "name" tokens outside of the object, and entire CRS objects in features.
            //we must not accidentally parse these objects as our main CRS object, but we do not know the type we should deserialize as. We will just cast to a string and parse the object again since this is not a big string.
            var CRSObject = serializer.Deserialize(reader);
            var CRSString = CRSObject.ToString();

            if (CRSString.Contains("link"))
            {
                var node = JSONNode.Parse(CRSString); //.DeserializeObject<LinkedCRS>(test);
                var href = node["properties"]["href"];
                var type = node["properties"]["type"];

                CRS = new LinkedCRS(href, type);
            }
            else if (CRSString.Contains("name"))
            {
                var node = JSONNode.Parse(CRSString); //.DeserializeObject<LinkedCRS>(test);
                var name = node["properties"]["name"];

                CRS = new NamedCRS(name);
            }

            print(CRS.Type);
            return;

            while (reader.Read())
            {
                print("finding crs " + reader.Value);
                if (reader.TokenType == JsonToken.PropertyName && IsAtTypeToken(reader))
                {
                    reader.Read(); // read the value of the type token.
                    if (reader.Value.ToString().ToLower() == "name")
                    {
                        // CRS = new NamedCRS(reader.Value.ToString());
                        CRS = serializer.Deserialize<NamedCRS>(reader);
                        break;
                    }

                    if (reader.Value.ToString().ToLower() == "link")
                    {
                        // CRS = new LinkedCRS(reader.Value.ToString());
                        CRS = serializer.Deserialize<LinkedCRS>(reader);
                        break;
                    }

                    // serializer.Deserialize<UnspecifiedCRS>(reader);
                    break;
                }
            }

            print("parsed CRS: " + CRS);
        }

        private void ReadType(JsonTextReader reader, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.PropertyName && IsAtTypeToken(reader))
            {
                reader.Read(); //read the value of the Type Object
                Type = serializer.Deserialize<GeoJSONObjectType>(reader);
                print("parsed type: " + Type);
            }
        }


        private static bool IsAtTypeToken(JsonTextReader reader)
        {
            return reader.Value.ToString().ToLower() == "type";
        }

        private static bool IsAtCRSToken(JsonTextReader reader)
        {
            return reader.Value.ToString().ToLower() == "crs";
        }

        private static bool IsAtFeaturesToken(JsonTextReader reader)
        {
            return reader.Value.ToString().ToLower() == "features";
        }
    }
}