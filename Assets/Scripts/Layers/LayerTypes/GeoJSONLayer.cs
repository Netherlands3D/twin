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
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using SimpleJSON;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class GeoJSONLayer : LayerNL3DBase
    {
        public static float maxParseDuration = 0.01f;

        public GeoJSONObjectType Type { get; private set; }
        public CRSBase CRS { get; private set; }
        public List<Feature> Features = new();

        private GeoJSONPolygonLayer polygonFeatures;
        private Material defaultVisualizationMaterial;

        private GeoJSONLineLayer lineFeatures;
        private LineRenderer3D lineRenderer3DPrefab; //todo: set this in the inspector somehow

        public UnityEvent<string> OnParseError = new();

        private GeoJSONPointLayer pointFeatures;
        private BatchedMeshInstanceRenderer pointRenderer3DPrefab;

        protected override void Start()
        {
            base.Start();
        }

        public void SetDefaultVisualizerSettings(Material defaultVisualizationMaterial, LineRenderer3D lineRenderer3DPrefab, BatchedMeshInstanceRenderer pointRenderer3DPrefab)
        {
            this.defaultVisualizationMaterial = defaultVisualizationMaterial;
            var layerColor = defaultVisualizationMaterial.color;
            layerColor.a = 1f;
            Color = layerColor;
            this.lineRenderer3DPrefab = lineRenderer3DPrefab;
            this.pointRenderer3DPrefab = pointRenderer3DPrefab;
        }

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
        }

        public void ParseGeoJSON(string filePath)
        {
            StartCoroutine(ParseGeoJSON(filePath, 1000));
        }

        private IEnumerator ParseGeoJSON(string filePath, int maxParsesPerFrame = Int32.MaxValue)
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
                    yield return ReadFeaturesArray(jsonReader, serializer);
                }
            }

            jsonReader.Close();

            var frameCount = Time.frameCount - startFrame;
            print(Features.Count + " features parsed and visualized: " + " in " + frameCount + " frames");

            if (frameCount == 0)
                yield return null; // if entire file was parsed in a single frame, we need to wait a frame to initialize UI to be able to set the color.
        }

        private void OnSerializerError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
        {
            OnParseError.Invoke("Er was een probleem met het inladen van dit GeoJSON bestand:\n\n" + args.ErrorContext.Error.Message);
        }

        private IEnumerator ReadFeaturesArray(JsonTextReader jsonReader, JsonSerializer serializer)
        {
            Features = new List<Feature>();
            var startTime = Time.realtimeSinceStartup;

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.EndArray)
                {
                    // end of feature array, stop parsing here
                    break;
                }

                var feature = serializer.Deserialize<Feature>(jsonReader);
                Features.Add(feature);
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
            var go = new GameObject("Polygonen");
            var layer = go.AddComponent<GeoJSONPolygonLayer>();
            layer.Color = Color;
            StartCoroutine(SetSubLayerParent(layer));
            layer.PolygonVisualizationMaterial = defaultVisualizationMaterial;
            return layer;
        }

        private GeoJSONLineLayer CreateLineLayer()
        {
            var go = new GameObject("Lijnen");
            var layer = go.AddComponent<GeoJSONLineLayer>();
            layer.LineRenderer3D = Instantiate(lineRenderer3DPrefab);
            layer.LineRenderer3D.LineMaterial = defaultVisualizationMaterial;
            layer.Color = Color;
            StartCoroutine(SetSubLayerParent(layer));
            return layer;
        }

        private GeoJSONPointLayer CreatePointLayer()
        {
            var go = new GameObject("Punten");
            var layer = go.AddComponent<GeoJSONPointLayer>();
            layer.PointRenderer3D = Instantiate(pointRenderer3DPrefab);
            layer.PointRenderer3D.Material = defaultVisualizationMaterial;
            layer.Color = Color;
            StartCoroutine(SetSubLayerParent(layer));
            return layer;
        }

        private IEnumerator SetSubLayerParent(LayerNL3DBase layer)
        {
            yield return null; //wait a frame for layer to be initialized
            layer.SetParent(this);
        }
        
        private void VisualizeFeature(Feature feature)
        {
            var originalCoordinateSystem = GetCoordinateSystem();
            switch (feature.Geometry.Type)
            {
                case GeoJSONObjectType.MultiPolygon:
                {
                    if (!polygonFeatures)
                        polygonFeatures = CreatePolygonLayer();

                    polygonFeatures.AddAndVisualizeFeature(feature, feature.Geometry as MultiPolygon, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.Polygon:
                {
                    if (!polygonFeatures)
                        polygonFeatures = CreatePolygonLayer();

                    polygonFeatures.AddAndVisualizeFeature(feature, feature.Geometry as Polygon, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.MultiLineString:
                {
                    if (!lineFeatures)
                        lineFeatures = CreateLineLayer();

                    lineFeatures.AddAndVisualizeFeature(feature, feature.Geometry as MultiLineString, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.LineString:
                {
                    if (!lineFeatures)
                        lineFeatures = CreateLineLayer();

                    lineFeatures.AddAndVisualizeFeature(feature, feature.Geometry as LineString, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.MultiPoint:
                {
                    if (!pointFeatures)
                        pointFeatures = CreatePointLayer();

                    pointFeatures.AddAndVisualizeFeature(feature, feature.Geometry as MultiPoint, originalCoordinateSystem);
                    break;
                }
                case GeoJSONObjectType.Point:
                {
                    if (!pointFeatures)
                        pointFeatures = CreatePointLayer();
                    
                    pointFeatures.AddAndVisualizeFeature(feature, feature.Geometry as Point, originalCoordinateSystem);
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