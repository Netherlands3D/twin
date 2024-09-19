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
using Netherlands3D.Twin.Layers.Properties;
using System.Linq;
using netDxf.Tables;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.Layers
{
    public class GeoJsonLayerGameObject : LayerGameObject, ILayerWithPropertyData
    {
        public static float maxParseDuration = 0.01f;
        
        public GeoJSONObjectType Type { get; private set; }
        public CRSBase CRS { get; private set; }

        private GeoJSONPolygonLayer polygonFeaturesLayer;
        private GeoJSONLineLayer lineFeaturesLayer;
        private GeoJSONPointLayer pointFeaturesLayer;
        
        [Header("Visualizer settings")]
        [SerializeField] private int maxFeatureVisualsPerFrame = 20;
        [SerializeField] private GeoJSONPolygonLayer polygonLayerPrefab;
        [SerializeField] private GeoJSONLineLayer lineLayerPrefab;
        [SerializeField] private GeoJSONPointLayer pointLayerPrefab;
        
        [SerializeField] private bool randomizeColorPerFeature = false;
        public bool RandomizeColorPerFeature { get => randomizeColorPerFeature; set => randomizeColorPerFeature = value; }
        public int MaxFeatureVisualsPerFrame { get => maxFeatureVisualsPerFrame; set => maxFeatureVisualsPerFrame = value; }

        [Space]
        public UnityEvent<string> OnParseError = new();
        private Coroutine streamParseCoroutine;
        protected LayerURLPropertyData urlPropertyData = new();
        LayerPropertyData ILayerWithPropertyData.PropertyData => urlPropertyData;

        protected virtual void Awake()
        {
            LoadDefaultValues();
        }

        protected virtual void LoadDefaultValues()
        {
            //GeoJSON layer+visual colors are set to random colors until user can pick colors in UI
            var randomLayerColor = Color.HSVToRGB(UnityEngine.Random.value, UnityEngine.Random.Range(0.5f, 1f), 1);
            randomLayerColor.a = 0.5f;
            LayerData.Color = randomLayerColor;
        }

        /// <summary>
        /// Load properties is only used when restoring a layer from a project file.
        /// After getting the property containing the url, the GeoJSON file is downloaded and parsed.
        /// </summary>
        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            LoadDefaultValues();

            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
                StartCoroutine(RestoreGeoJsonLocalFile(urlProperty.url));
            }
        }

        /// <summary>
        /// Removes features based on the bounds of their visualisations
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {
            if (polygonFeaturesLayer != null)
                polygonFeaturesLayer.RemoveFeaturesOutOfView();

            if (lineFeaturesLayer != null)
                lineFeaturesLayer.RemoveFeaturesOutOfView();

            if (pointFeaturesLayer != null)
                pointFeaturesLayer.RemoveFeaturesOutOfView();
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
        /// Sets URL and start a 'streaming' parse of the GeoJSON file. This will spread out the generation of visuals over multiple frames.
        /// Ideal for large single files.
        /// </summary>
        public void SetURL(string path, string sourceUrl = "")
        {
            this.urlPropertyData.url = sourceUrl;

            if (streamParseCoroutine != null)
                StopCoroutine(streamParseCoroutine);

            streamParseCoroutine = StartCoroutine(ParseGeoJSONStream(path, 1000));
        }

        private IEnumerator RestoreGeoJsonLocalFile(string url)
        {
            //create LocalFile so we can use it in the ParseGeoJSONStream function
            var uwr = UnityWebRequest.Get(url);
            var optionalExtention = Path.GetExtension(url).Split("?")[0];
            var guidFilename = Guid.NewGuid().ToString() + optionalExtention;
            string path = Path.Combine(Application.persistentDataPath, guidFilename);

            uwr.downloadHandler = new DownloadHandlerFile(path);
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                SetURL(path);
            }
            else
            {
                OnParseError.Invoke("Dit GeoJSON bestand kon niet worden ingeladen vanaf de URL.");
            }
        }

        private IEnumerator ParseGeoJSONStream(string path, int maxParsesPerFrame = Int32.MaxValue)
        {
            var startFrame = Time.frameCount;
            var reader = new StreamReader(path);
            var jsonReader = new JsonTextReader(reader);

            JsonSerializer serializer = new JsonSerializer();
            serializer.Error += OnSerializerError;

            FindTypeAndCRS(jsonReader, serializer);

            //reset position of reader
            jsonReader.Close();
            reader = new StreamReader(path);
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

        private GeoJSONPolygonLayer CreateOrGetPolygonLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if(child is ReferencedLayerData referencedLayerData)
                {
                    if(referencedLayerData.Reference is GeoJSONPolygonLayer polygonLayer)
                        return polygonLayer;
                }
            }

            GeoJSONPolygonLayer newPolygonLayerGameObject = Instantiate(polygonLayerPrefab);
            newPolygonLayerGameObject.LayerData.Color = LayerData.Color;
            newPolygonLayerGameObject.LayerData.SetParent(LayerData);
            return newPolygonLayerGameObject;
        }

        private GeoJSONLineLayer CreateOrGetLineLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if(child is ReferencedLayerData referencedLayerData)
                {
                    if(referencedLayerData.Reference is GeoJSONLineLayer lineLayer)
                        return lineLayer;
                }
            }

            GeoJSONLineLayer newLineLayerGameObject = Instantiate(lineLayerPrefab);
            newLineLayerGameObject.LayerData.Color = LayerData.Color;

            var lineMaterial = new Material(newLineLayerGameObject.LineRenderer3D.LineMaterial) { color = LayerData.Color };
            newLineLayerGameObject.LineRenderer3D.LineMaterial = lineMaterial;

            newLineLayerGameObject.LayerData.SetParent(LayerData);
            return newLineLayerGameObject;
        }

        private GeoJSONPointLayer CreateOrGetPointLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if(child is ReferencedLayerData referencedLayerData)
                {
                    if(referencedLayerData.Reference is GeoJSONPointLayer pointLayer)
                        return pointLayer;
                }
            }

            GeoJSONPointLayer newPointLayerGameObject = Instantiate(pointLayerPrefab);
            newPointLayerGameObject.LayerData.Color = LayerData.Color;

            var pointMaterial = new Material(newPointLayerGameObject.PointRenderer3D.Material) { color = LayerData.Color };
            newPointLayerGameObject.PointRenderer3D.Material = pointMaterial;

            newPointLayerGameObject.LayerData.SetParent(LayerData);

            return newPointLayerGameObject;
        }
        
        private void VisualizeFeature(Feature feature)
        {
            var originalCoordinateSystem = GetCoordinateSystem();
            switch (feature.Geometry.Type)
            {
                case GeoJSONObjectType.MultiPolygon:
                    AddMultiPolygonFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.Polygon:
                    AddPolygonFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.MultiLineString:
                    AddMultiLineStringFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.LineString:
                    AddLineStringFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.MultiPoint:
                    AddMultiPointFeature(feature, originalCoordinateSystem);
                    return;
                case GeoJSONObjectType.Point:
                    AddPointFeature(feature, originalCoordinateSystem);
                    return;
                default:
                    throw new InvalidCastException("Features of type " + feature.Geometry.Type + " are not supported for visualization");
            }
        }

        private void AddPointFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (pointFeaturesLayer == null)
                pointFeaturesLayer = CreateOrGetPointLayer();

            pointFeaturesLayer.AddAndVisualizeFeature<Point>(feature, originalCoordinateSystem);
        }

        private void AddMultiPointFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (pointFeaturesLayer == null)
                pointFeaturesLayer = CreateOrGetPointLayer();

            pointFeaturesLayer.AddAndVisualizeFeature<MultiPoint>(feature, originalCoordinateSystem);
        }

        private void AddLineStringFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (lineFeaturesLayer == null)
                lineFeaturesLayer = CreateOrGetLineLayer();

            lineFeaturesLayer.AddAndVisualizeFeature<MultiLineString>(feature, originalCoordinateSystem);
        }

        private void AddMultiLineStringFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (lineFeaturesLayer == null)
                lineFeaturesLayer = CreateOrGetLineLayer();

            lineFeaturesLayer.AddAndVisualizeFeature<MultiLineString>(feature, originalCoordinateSystem);
        }

        private void AddPolygonFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (polygonFeaturesLayer == null)
                polygonFeaturesLayer = CreateOrGetPolygonLayer();

            polygonFeaturesLayer.AddAndVisualizeFeature<Polygon>(feature, originalCoordinateSystem);
        }

        private void AddMultiPolygonFeature(Feature feature, CoordinateSystem originalCoordinateSystem)
        {
            if (polygonFeaturesLayer == null)
                polygonFeaturesLayer = CreateOrGetPolygonLayer();

            polygonFeaturesLayer.AddAndVisualizeFeature<MultiPolygon>(feature, originalCoordinateSystem);
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