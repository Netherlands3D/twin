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
using Netherlands3D.Twin.Projects.ExtensionMethods;
using UnityEngine.Networking;
using Netherlands3D.SubObjects;

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
        protected LayerURLPropertyData urlPropertyData = new();
        public LayerPropertyData PropertyData => urlPropertyData;

        protected virtual void Awake()
        {
            LoadDefaultValues();
        }

        protected override void Start()
        {
            base.Start();
            if(urlPropertyData.Data.IsStoredInProject())
                StartCoroutine(ParseGeoJSONStreamLocal(urlPropertyData.Data, 1000));
            else if(urlPropertyData.Data.IsRemoteAsset())
                StartCoroutine(ParseGeoJSONStreamRemote(urlPropertyData.Data, 1000));
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
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
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
                ProcessFeatureMapping(feature);

                if (i % MaxFeatureVisualsPerFrame == 0)
                    yield return null;
            }
        }

        private IEnumerator ParseGeoJSONStreamRemote(Uri uri, int maxParsesPerFrame = Int32.MaxValue)
        {
            //create LocalFile so we can use it in the ParseGeoJSONStream function
            string url = uri.ToString();
            var uwr = UnityWebRequest.Get(url);

            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                var startFrame = Time.frameCount;
                // Get the downloaded text
                string jsonText = uwr.downloadHandler.text;
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

                while (jsonReader.Read())
                {
                    // Read features depending on type
                    if (jsonReader.TokenType == JsonToken.PropertyName && IsAtFeaturesToken(jsonReader))
                    {
                        jsonReader.Read(); // Start array
                        yield return ReadFeaturesArrayStream(jsonReader, serializer);
                    }
                }
                jsonReader.Close();

                var frameCount = Time.frameCount - startFrame;
                if (frameCount == 0)
                    yield return null; // if entire file was parsed in a single frame, we need to wait a frame to initialize UI to be able to set the color.
            }
            else
            {
                OnParseError.Invoke("Dit GeoJSON bestand kon niet worden ingeladen vanaf de URL.");
            }
        }

        private IEnumerator ParseGeoJSONStreamLocal(Uri uri, int maxParsesPerFrame = Int32.MaxValue)
        {
            string path = Path.Combine(Application.persistentDataPath, uri.LocalPath.TrimStart('/', '\\')); 
          
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
                ProcessFeatureMapping(feature);

                var parseDuration = Time.realtimeSinceStartup - startTime;
                if (parseDuration > maxParseDuration)
                {
                    yield return null;
                    startTime = Time.realtimeSinceStartup;
                }
            }
        }

        private void ProcessFeatureMapping(Feature feature)
        {
            var polygonData = polygonFeaturesLayer?.GetMeshData(feature);
            if (polygonData != null)
            {
                CreateFeatureMappings(polygonFeaturesLayer, feature, polygonData);
            }
            var lineData = lineFeaturesLayer?.GetMeshData(feature);
            if (lineData != null)
            {
                CreateFeatureMappings(lineFeaturesLayer, feature, lineData);
            }
            var pointData = pointFeaturesLayer?.GetMeshData(feature);
            if (pointData != null)
            {
                CreateFeatureMappings(pointFeaturesLayer, feature, pointData);
            }          
        }

        private void CreateFeatureMappings(IGeoJsonVisualisationLayer layer, Feature feature, List<Mesh> meshes)
        {
            for(int i = 0; i < meshes.Count; i++)
            {
                Mesh mesh = meshes[i];                
                Vector3[] verts = mesh.vertices;                
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                float width = 1f;
                GameObject subObject = new GameObject(feature.Geometry.ToString() + "_submesh_" + layer.Transform.transform.childCount.ToString());
                subObject.AddComponent<MeshFilter>().mesh = mesh;
                if (verts.Length >= 2)
                {
                    //generate collider extruded lines for lines
                    if (feature.Geometry is MultiLineString || feature.Geometry is LineString)
                    {
                        GeoJSONLineLayer lineLayer = layer as GeoJSONLineLayer;
                        width = lineLayer.LineRenderer3D.LineDiameter;

                        for (int j = 0; j < verts.Length - 1; j++)
                        {
                            Vector3 p1 = verts[j];
                            Vector3 p2 = verts[j + 1];
                            Vector3 edgeDir = (p2 - p1).normalized;
                            Vector3 perpDir = new Vector3(edgeDir.z, 0, -edgeDir.x);
                            Vector3 v1 = p1 + perpDir * width;
                            Vector3 v2 = p1 - perpDir * width;
                            Vector3 v3 = p2 + perpDir * width;
                            Vector3 v4 = p2 - perpDir * width;
                            vertices.Add(v1); //tl
                            vertices.Add(v2); //bl
                            vertices.Add(v3); //tr
                            vertices.Add(v4); //br

                            int baseIndex = j * 4;
                            //v1 v2 v3
                            triangles.Add(baseIndex + 0);
                            triangles.Add(baseIndex + 1);
                            triangles.Add(baseIndex + 2);
                            //v2 v4 v3
                            triangles.Add(baseIndex + 2);
                            triangles.Add(baseIndex + 1);
                            triangles.Add(baseIndex + 3);
                        }
                        mesh.vertices = vertices.ToArray();
                        mesh.triangles = triangles.ToArray();
                        subObject.AddComponent<MeshCollider>();
                        subObject.AddComponent<MeshRenderer>().material = lineLayer.LineRenderer3D.LineMaterial;
                    }
                    else if (feature.Geometry is MultiPolygon || feature.Geometry is Polygon)
                    {
                        //lets not add a meshcollider since its very heavy
                    }                   
                }
                else
                {
                    if (feature.Geometry is Point || feature.Geometry is MultiPoint)
                    {
                        subObject.transform.position = verts[0];
                        GeoJSONPointLayer pointLayer = layer as GeoJSONPointLayer;
                        subObject.AddComponent<SphereCollider>().radius = pointLayer.PointRenderer3D.MeshScale * 0.5f;

                    }
                }

                               
                mesh.RecalculateBounds();
                meshes[i] = mesh;

                subObject.transform.SetParent(layer.Transform);
                subObject.layer = LayerMask.NameToLayer("Projected");

                FeatureMapping objectMapping = subObject.AddComponent<FeatureMapping>();
                objectMapping.SetFeature(feature);
                objectMapping.SetMeshes(meshes);
                objectMapping.SetVisualisationLayer(layer);
                objectMapping.SetGeoJsonLayerParent(this);
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