using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GeoJSON.Net;
using GeoJSON.Net.CoordinateReferenceSystem;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using System.Linq;
using Netherlands3D.Twin.Projects.ExtensionMethods;

namespace Netherlands3D.Twin.Layers
{
    public class GeoJsonLayerGameObject : LayerGameObject, ILayerWithPropertyData
    {
        private GeoJsonParser parser = new GeoJsonParser(0.01f);
        public GeoJsonParser Parser => parser;

        private GeoJSONPolygonLayer polygonFeaturesLayer;
        private GeoJSONLineLayer lineFeaturesLayer;
        private GeoJSONPointLayer pointFeaturesLayer;
        
        [Header("Visualizer settings")]
        [SerializeField] private int maxFeatureVisualsPerFrame = 20;
        [SerializeField] private GeoJSONPolygonLayer polygonLayerPrefab;
        [SerializeField] private GeoJSONLineLayer lineLayerPrefab;
        [SerializeField] private GeoJSONPointLayer pointLayerPrefab;
        
        public int MaxFeatureVisualsPerFrame { get => maxFeatureVisualsPerFrame; set => maxFeatureVisualsPerFrame = value; }

        [Space]
        protected LayerURLPropertyData urlPropertyData = new();
        public LayerPropertyData PropertyData => urlPropertyData;

        private void Awake()
        {
            parser.OnFeatureParsed.AddListener(AddFeatureVisualisation);
        }
        
        protected override void Start()
        {
            base.Start();

            if (urlPropertyData.Data.IsStoredInProject())
            {
                string path = Path.Combine(Application.persistentDataPath, urlPropertyData.Data.LocalPath.TrimStart('/', '\\'));
                StartCoroutine(parser.ParseGeoJSONLocal(path));
            }
            else if (urlPropertyData.Data.IsRemoteAsset())
            {
                StartCoroutine(parser.ParseGeoJSONStreamRemote(urlPropertyData.Data));
            }
        }

        private void OnDestroy()
        {
            parser.OnFeatureParsed.RemoveListener(AddFeatureVisualisation);
        }

        public void AddFeatureVisualisation(Feature feature)
        {
            VisualizeFeature(feature);
            ProcessFeatureMapping(feature);
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
                        float halfWidth = width * 0.5f;

                        int segmentCount = verts.Length - 1;
                        int vertexCount = segmentCount * 4;  // 4 vertices per segment
                        int triangleCount = segmentCount * 6; // 2 triangles per segment, 3 vertices each

                        Vector3[] vertices = new Vector3[vertexCount];
                        int[] triangles = new int[triangleCount];

                        for (int j = 0; j < segmentCount; j++)
                        {
                            Vector3 p1 = verts[j];
                            Vector3 p2 = verts[j + 1];
                            Vector3 edgeDir = (p2 - p1).normalized;
                            Vector3 perpDir = new Vector3(edgeDir.z, 0, -edgeDir.x);

                            Vector3 v1 = p1 + perpDir * halfWidth;
                            Vector3 v2 = p1 - perpDir * halfWidth;
                            Vector3 v3 = p2 + perpDir * halfWidth;
                            Vector3 v4 = p2 - perpDir * halfWidth;

                            int baseIndex = j * 4;
                            vertices[baseIndex + 0] = v1; // Top left
                            vertices[baseIndex + 1] = v2; // Bottom left
                            vertices[baseIndex + 2] = v3; // Top right
                            vertices[baseIndex + 3] = v4; // Bottom right

                            int triBaseIndex = j * 6;
                            // Triangle 1
                            triangles[triBaseIndex + 0] = baseIndex + 0;
                            triangles[triBaseIndex + 1] = baseIndex + 1;
                            triangles[triBaseIndex + 2] = baseIndex + 2;

                            // Triangle 2
                            triangles[triBaseIndex + 3] = baseIndex + 2;
                            triangles[triBaseIndex + 4] = baseIndex + 1;
                            triangles[triBaseIndex + 5] = baseIndex + 3;
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
                if (child is not ReferencedLayerData referencedLayerData) continue;
                if (referencedLayerData.Reference is not GeoJSONPolygonLayer polygonLayer) continue;

                return polygonLayer;
            }

            GeoJSONPolygonLayer newPolygonLayerGameObject = Instantiate(polygonLayerPrefab);
            newPolygonLayerGameObject.LayerData.Color = LayerData.Color;

            // Replace default style with the parent's default style
            newPolygonLayerGameObject.LayerData.RemoveStyle(newPolygonLayerGameObject.LayerData.DefaultStyle);
            newPolygonLayerGameObject.LayerData.AddStyle(LayerData.DefaultStyle);

            newPolygonLayerGameObject.LayerData.SetParent(LayerData);
            
            return newPolygonLayerGameObject;
        }

        private GeoJSONLineLayer CreateOrGetLineLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if (child is not ReferencedLayerData referencedLayerData) continue;
                if (referencedLayerData.Reference is not GeoJSONLineLayer lineLayer) continue;

                return lineLayer;
            }

            GeoJSONLineLayer newLineLayerGameObject = Instantiate(lineLayerPrefab);
            newLineLayerGameObject.LayerData.Color = LayerData.Color;

            // Replace default style with the parent's default style
            newLineLayerGameObject.LayerData.RemoveStyle(newLineLayerGameObject.LayerData.DefaultStyle);
            newLineLayerGameObject.LayerData.AddStyle(LayerData.DefaultStyle);

            newLineLayerGameObject.LayerData.SetParent(LayerData);
            return newLineLayerGameObject;
        }

        private GeoJSONPointLayer CreateOrGetPointLayer()
        {
            var childrenInLayerData = LayerData.ChildrenLayers;
            foreach (var child in childrenInLayerData)
            {
                if (child is not ReferencedLayerData referencedLayerData) continue;
                if (referencedLayerData.Reference is not GeoJSONPointLayer pointLayer) continue;

                return pointLayer;
            }

            GeoJSONPointLayer newPointLayerGameObject = Instantiate(pointLayerPrefab);
            newPointLayerGameObject.LayerData.Color = LayerData.Color;

            // Replace default style with the parent's default style
            newPointLayerGameObject.LayerData.RemoveStyle(newPointLayerGameObject.LayerData.DefaultStyle);
            newPointLayerGameObject.LayerData.AddStyle(LayerData.DefaultStyle);

            newPointLayerGameObject.LayerData.SetParent(LayerData);

            return newPointLayerGameObject;
        }
        
        private void VisualizeFeature(Feature feature)
        {
            var originalCoordinateSystem = GeoJsonParser.GetCoordinateSystem(feature.CRS);
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
    }
}