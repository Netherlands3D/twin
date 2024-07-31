using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using GeoJSON.Net;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using SimpleJSON;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers
{
    public class GeoJSONLayer : LayerGameObject
    {
        public static float maxParseDuration = 0.01f;
        
        private int maxFeatureVisualsPerFrame = 20;

        public GeoJSONObjectType Type { get; private set; }
        public CRSBase CRS { get; private set; }

        private GeoJSONPolygonLayer polygonFeatures;
        private Material defaultVisualizationMaterial;
        private bool randomizeColorPerFeature = false;
        public bool RandomizeColorPerFeature { get => randomizeColorPerFeature; set => randomizeColorPerFeature = value; }
        public int MaxFeatureVisualsPerFrame { get => maxFeatureVisualsPerFrame; set => maxFeatureVisualsPerFrame = value; }

        private GeoJSONLineLayer lineFeatures;
        private LineRenderer3D lineRenderer3DPrefab;

        public UnityEvent<string> OnParseError = new();

        private GeoJSONPointLayer pointFeatures;
        private BatchedMeshInstanceRenderer pointRenderer3DPrefab;
        
        public void SetDefaultVisualizerSettings(Material defaultVisualizationMaterial, LineRenderer3D lineRenderer3DPrefab, BatchedMeshInstanceRenderer pointRenderer3DPrefab)
        {
            this.defaultVisualizationMaterial = defaultVisualizationMaterial;
            var layerColor = defaultVisualizationMaterial.color;
            layerColor.a = 1f;
            LayerData.Color = layerColor;
            this.lineRenderer3DPrefab = lineRenderer3DPrefab;
            this.pointRenderer3DPrefab = pointRenderer3DPrefab;
        }

        /// <summary>
        /// Removes features based on the bounds of their visualisations
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {
            if (polygonFeatures != null)
                polygonFeatures.RemoveFeaturesOutOfView();

            if (lineFeatures != null)
                lineFeatures.RemoveFeaturesOutOfView();

            if (pointFeatures != null)
                pointFeatures.RemoveFeaturesOutOfView();
        }

        public void AppendFeatureCollection(FeatureCollection featureCollection)
        {
            var collectionCRS = featureCollection.CRS;
            //Determine if CRS is LinkedCRS or NamedCRS
            if (collectionCRS is NamedCRS)
            {
                if (CoordinateSystems.FindCoordinateSystem((collectionCRS as NamedCRS).Properties["name"].ToString(), out var globalCoordinateSystem))
                {
                    CRS = collectionCRS as NamedCRS;
                }
            }
            else if (collectionCRS is LinkedCRS)
            {
                Debug.LogError("Linked CRS parsing is currently not supported, using default CRS (WGS84) instead"); //todo: implement this
                return;
            }

            StartCoroutine(VisualizeQueue(featureCollection.Features));
        }

        IEnumerator VisualizeQueue(List<Feature> features)
        {
            for (int i = 0; i < features.Count; i++)
            {
                var feature = features[i];

                //If a feature was not found, stop queue
                if (feature == null)
                    yield break;
                
                VisualizeFeature(feature);

                if (i % MaxFeatureVisualsPerFrame == 0)
                    yield return null;
            }
        }

        /// <summary>
        /// Parses a GeoJSON files and updates the exisiting list of Features with the new features.
        /// Ideal of you want to build a visualisation of multiple GeoJSON files (like tiled request using bbox)
        /// </summary>
        /// <param name="filePath"></param>
        public void AdditiveParseGeoJSON(string filePath)
        {
            // Read filepath and deserialize the GeoJSON using GeoJSON.net in one go
            var jsonText = File.ReadAllText(filePath);
            var featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(jsonText);
            
            AppendFeatureCollection(featureCollection);
        }

        /// <summary>
        /// Start a 'streaming' parse of a GeoJSON file. This will spread out the generation of visuals over multiple frames.
        /// Ideal for large single files.
        /// </summary>
        /// <param name="filePath"></param>
        public void StreamParseGeoJSON(string filePath)
        {
            StartCoroutine(ParseGeoJSONStream(filePath, 1000));
        }

        private IEnumerator ParseGeoJSONStream(string filePath, int maxParsesPerFrame = Int32.MaxValue)
        {
            var startFrame = Time.frameCount;
            var reader = new StreamReader(filePath);
            var jsonReader = new JsonTextReader(reader);

            JsonSerializer serializer = new JsonSerializer();
            serializer.Error += OnSerializerError;

            FindTypeAndCRS(jsonReader, serializer);

            //reset position of reader
            jsonReader.Close();
            reader = new StreamReader(filePath);
            jsonReader = new JsonTextReader(reader);

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
                features.Add(feature);
                VisualizeFeature(feature);

                var parseDuration = Time.realtimeSinceStartup - startTime;
                if (parseDuration > maxParseDuration)
                {
                    yield return null;
                    startTime = Time.realtimeSinceStartup;
                }
            }
        }

        private GeoJSONPolygonLayer CreatePolygonLayer()
        {
            var layer = new GeoJSONPolygonLayer("Polygonen")
            {
                Color = LayerData.Color,
                PolygonVisualizationMaterial = defaultVisualizationMaterial,
                RandomizeColorPerFeature = RandomizeColorPerFeature
            };
            layer.SetParent(LayerData);
            return layer;
        }

        private GeoJSONLineLayer CreateLineLayer()
        {
            var layer = new GeoJSONLineLayer("Lijnen");
            layer.LineRenderer3D = Instantiate(lineRenderer3DPrefab);
            layer.LineRenderer3D.LineMaterial = defaultVisualizationMaterial;
            layer.Color = LayerData.Color;
            layer.SetParent(LayerData);
            return layer;
        }

        private GeoJSONPointLayer CreatePointLayer()
        {
            var layer = new GeoJSONPointLayer("Punten");
            layer.PointRenderer3D = Instantiate(pointRenderer3DPrefab);
            layer.PointRenderer3D.Material = defaultVisualizationMaterial;
            layer.Color = LayerData.Color;
            layer.SetParent(LayerData);
            return layer;
        }
        
        private void VisualizeFeature(Feature feature)
        {
            var originalCoordinateSystem = GetCoordinateSystem();
            switch (feature.Geometry.Type)
            {
                case GeoJSONObjectType.MultiPolygon:
                {
                    if (polygonFeatures == null)
                        polygonFeatures = CreatePolygonLayer();

                    polygonFeatures.AddAndVisualizeFeature<MultiPolygon>(feature, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.Polygon:
                {
                    if (polygonFeatures == null)
                        polygonFeatures = CreatePolygonLayer();

                    polygonFeatures.AddAndVisualizeFeature<Polygon>(feature, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.MultiLineString:
                {
                    if (lineFeatures == null)
                        lineFeatures = CreateLineLayer();

                    lineFeatures.AddAndVisualizeFeature<MultiLineString>(feature, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.LineString:
                {
                    if (lineFeatures == null)
                        lineFeatures = CreateLineLayer();

                    lineFeatures.AddAndVisualizeFeature<MultiLineString>(feature, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.MultiPoint:
                {
                    if (pointFeatures == null)
                        pointFeatures = CreatePointLayer();

                    pointFeatures.AddAndVisualizeFeature<MultiPoint>(feature, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.Point:
                {
                    if (pointFeatures == null)
                        pointFeatures = CreatePointLayer();
                    
                    pointFeatures.AddAndVisualizeFeature<Point>(feature, originalCoordinateSystem);
                    break;
                }
                default:
                {
                    throw new InvalidCastException("Features of type " + feature.Geometry.Type + " are not supported for visualization");
                }
            }
        }

        private CoordinateSystem GetCoordinateSystem()
        {
            var coordinateSystem = CoordinateSystem.CRS84;

            if (CRS is NamedCRS)
            {
                if (CoordinateSystems.FindCoordinateSystem((CRS as NamedCRS).Properties["name"].ToString(), out var globalCoordinateSystem))
                {
                    coordinateSystem = globalCoordinateSystem;
                }
            }
            else if (CRS is LinkedCRS)
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
                Debug.Log("Token: " + reader.TokenType + " Value: " + reader.Value);

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
                // print("parsed type: " + Type);
            }
        }


        private static bool IsAtTypeToken(JsonTextReader reader)
        {
            if(reader.TokenType != JsonToken.PropertyName)
                    return false;

            return reader.Value.ToString().ToLower() == "type";
        }

        private static bool IsAtCRSToken(JsonTextReader reader)
        {
            if(reader.TokenType != JsonToken.PropertyName)
                return false;

            return reader.Value.ToString().ToLower() == "crs";
        }

        private static bool IsAtFeaturesToken(JsonTextReader reader)
        {
            if(reader.TokenType != JsonToken.PropertyName)
                return false;

            return reader.Value.ToString().ToLower() == "features";
        }
    }
}