using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GeoJSON.Net;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Feature;
using KindMen.Uxios;
using KindMen.Uxios.Http;
using Netherlands3D.Coordinates;
using Netherlands3D.Credentials.StoredAuthorization;
using Newtonsoft.Json;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public class GeoJSONParser
    {
        public float maxParseDuration = 0.01f;
        public GeoJSONObjectType Type { get; private set; }
        public ICRSObject CRS { get; set; }

        [Space, Header("Parse events")] public UnityEvent<Feature> OnFeatureParsed = new();
        public UnityEvent<string> OnParseError = new();

        public GeoJSONParser(float maxParsePerFrameDuration)
        {
            maxParseDuration = maxParsePerFrameDuration;
        }

        public IEnumerator ParseJSONString(string jsonText)
        {
            // Get the downloaded text
            // string jsonText = uwr.downloadHandler.text;
            StringReader reader = new StringReader(jsonText);
            JsonTextReader jsonReader = new JsonTextReader(reader);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Error += OnSerializerError;

            FindTypeAndCRS(jsonReader, serializer);

            //reset position of reader
            jsonReader.Close();
            reader.Dispose();

            reader = new StringReader(jsonText); // Reset to start of JSON
            jsonReader = new JsonTextReader(reader);

            yield return ParseFeatures(jsonReader, serializer);
        }

        public IEnumerator ParseGeoJSONLocal(string path)
        {
            var reader = new StreamReader(path);
            var jsonReader = new JsonTextReader(reader);

            JsonSerializer serializer = new JsonSerializer();
            serializer.Error += OnSerializerError;

            FindTypeAndCRS(jsonReader, serializer);

            //reset position of reader
            jsonReader.Close();
            reader = new StreamReader(path);
            jsonReader = new JsonTextReader(reader);

            yield return ParseFeatures(jsonReader, serializer);
        }

        public IEnumerator ParseGeoJSONStreamRemote(StoredAuthorization auth)
        {
            var headers = new Headers();
            var customQueryParams = new KindMen.Uxios.Http.QueryParameters();

            switch (auth)
            {
                case HeaderBasedAuthorization headerBasedAuthorization:
                    var (headerName, headerValue) = headerBasedAuthorization.GetHeaderKeyAndValue();
                    headers.Add(headerName, headerValue);
                    break;
                case QueryStringAuthorization queryStringAuthorization:
                    customQueryParams.Add(queryStringAuthorization.QueryKeyName, queryStringAuthorization.QueryKeyValue);
                    break;
                case Public:
                    break; //nothing specific needed, but it needs to be excluded from default
                default:
                    throw new NotImplementedException("Credential type " + auth.GetType() + " is not supported by " + GetType());
            }

            var config = new Config
            {
                Headers = headers,
                Params = customQueryParams
            };

            string jsonString = string.Empty;

            var promise = Uxios.DefaultInstance.Get<string>(auth.InputUri, config);
            promise.Then(response => jsonString = response.Data as string
            );
            promise.Catch(response =>
                OnParseError.Invoke("Dit GeoJSON bestand kon niet worden ingeladen vanaf de URL: " + response.InnerException)
            );
            
            yield return Uxios.WaitForRequest(promise);

            if(!string.IsNullOrEmpty(jsonString))
                yield return ParseJSONString(jsonString);
        }

        private IEnumerator ParseFeatures(JsonTextReader jsonReader, JsonSerializer serializer)
        {
            var startFrame = Time.frameCount;
            while (jsonReader.Read())
            {
                //read features depending on type
                if (jsonReader.TokenType == JsonToken.PropertyName && IsAtFeaturesToken(jsonReader))
                {
                    jsonReader.Read(); //start array
                    yield return ReadFeaturesArrayStream(jsonReader, serializer);
                }
            }

            jsonReader.Close();

            var frameCount = Time.frameCount - startFrame;
            if (frameCount == 0)
                yield return null; // if entire file was parsed in a single frame, we need to wait a frame to initialize UI to be able to set the color.
        }

        private void OnSerializerError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
        {
            OnParseError.Invoke("Er was een probleem met het inladen van dit GeoJSON bestand:\n\n" + args.ErrorContext.Error.Message);
        }

        private IEnumerator ReadFeaturesArrayStream(JsonTextReader jsonReader, JsonSerializer serializer)
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
                if (feature.CRS == null)
                {
                    feature.CRS = CRS;
                }

                features.Add(feature);
                OnFeatureParsed.Invoke(feature);

                var parseDuration = Time.realtimeSinceStartup - startTime;
                if (parseDuration > maxParseDuration)
                {
                    yield return null;
                    startTime = Time.realtimeSinceStartup;
                }
            }
        }

        public static CoordinateSystem GetCoordinateSystem(ICRSObject crs)
        {
            var coordinateSystem = CoordinateSystem.CRS84;

            if (crs is NamedCRS namedCrs)
            {
                if (CoordinateSystems.FindCoordinateSystem(namedCrs.Properties["name"].ToString(), out var globalCoordinateSystem))
                {
                    coordinateSystem = globalCoordinateSystem;
                }
            }
            else if (crs is LinkedCRS linkedCrs)
            {
                Debug.LogError("Linked CRS parsing is currently not supported, using default CRS (WGS84) instead"); //todo: implement this
            }

            return coordinateSystem;
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
                // Log the current token
                // Debug.Log("Token: " + reader.TokenType + " Value: " + reader.Value);

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
        }

        private void ReadType(JsonTextReader reader, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.PropertyName && IsAtTypeToken(reader))
            {
                reader.Read(); //read the value of the Type Object
                Type = serializer.Deserialize<GeoJSONObjectType>(reader);
            }
        }

        private static bool IsAtTypeToken(JsonTextReader reader)
        {
            if (reader.TokenType != JsonToken.PropertyName)
                return false;

            return reader.Value.ToString().ToLower() == "type";
        }

        private static bool IsAtCRSToken(JsonTextReader reader)
        {
            if (reader.TokenType != JsonToken.PropertyName)
                return false;

            return reader.Value.ToString().ToLower() == "crs";
        }

        private static bool IsAtFeaturesToken(JsonTextReader reader)
        {
            if (reader.TokenType != JsonToken.PropertyName)
                return false;

            return reader.Value.ToString().ToLower() == "features";
        }
    }
}