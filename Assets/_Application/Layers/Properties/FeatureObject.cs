using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.layers.properties
{
    [RequireComponent(typeof(LayerGameObject))]
    public class FeatureObject : MonoBehaviour, IVisualizationWithPropertyData
    {
        private LayerGameObject visualization;
        private Dictionary<string, FeaturePropertyData.FeatureData> featureIds = new();
        private ObjectSelectorService selectorService;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            visualization = GetComponent<LayerGameObject>();
            visualization.InitProperty<FeaturePropertyData>(properties);
        }

        private void OnEnable()
        {
            selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            selectorService.SelectFeature.AddListener(ProcessFeatureMappingForLayer);
            selectorService.OnDeselect.AddListener(ClearFeatureMappingsForLayer);
        }

        private void OnDisable()
        {
            selectorService.SelectFeature.RemoveListener(ProcessFeatureMappingForLayer);
            selectorService.OnDeselect.RemoveListener(ClearFeatureMappingsForLayer);
        }

        private void ProcessFeatureMappingForLayer(FeatureMapping mapping)
        {
            //TODO this is begging for a refactor, we dont want to check the parent but until https://gemeente-amsterdam.atlassian.net/browse/S3DA-1935 this has to stay
            if (visualization == null || mapping == null || visualization.LayerData.ParentLayer != mapping.LayerData)
            {
                ClearFeatureMappingsForLayer();
                return;
            }

            FeaturePropertyData propertyData = visualization.LayerData.GetProperty<FeaturePropertyData>();
            featureIds.Clear();
            ObjectSelectorService selectorService = ServiceLocator.GetService<ObjectSelectorService>();
            foreach (KeyValuePair<string, IMapping> kv in selectorService.SelectedMappings)
            {
                if (kv.Value is FeatureMapping map)
                {
                    Bounds bounds = GetThumbnailBoundingBox(map);
                    BoundingBox bbox = new  BoundingBox(bounds);
                    Dictionary<string, object> properties = map.Feature.Properties as Dictionary<string, object>;
                    FeaturePropertyData.FeatureData data = new FeaturePropertyData.FeatureData();
                    data.Properties = properties;
                    data.BoundingBox = bbox;
                    featureIds.Add(kv.Key, data);
                }
            }
            propertyData.FeatureIds = featureIds;
        }
        
        private Bounds GetThumbnailBoundingBox(FeatureMapping mapping)
        {
            if (mapping.VisualisationLayer is GeoJSONPolygonLayer)
            {
                GeoJSONPolygonLayer polygonLayer = mapping.VisualisationLayer as GeoJSONPolygonLayer;
                List<Mesh> meshes = mapping.FeatureMeshes;

                //todo: Mapping.BoundingBox should be the bbox of all meshes in the feature, this is currently not working correctly.
                var center = mapping.BoundingBox.Center.ToUnity();
                var bounds = new Bounds(center, Vector3.zero);
                foreach (var mesh in meshes)
                {
	                PolygonVisualisation pv = polygonLayer.GetPolygonVisualisationByMesh(mesh);
                    Bounds currentObjectBounds = new Bounds(pv.transform.position, mesh.bounds.size);
	                bounds.Encapsulate(currentObjectBounds);
                }
				return bounds;
            }
            if (mapping.VisualisationLayer is GeoJSONLineLayer)
            {
                Vector3 centroid = Vector3.zero;
                Vector3[] vertices = mapping.FeatureMeshes[0].vertices;
                foreach (Vector3 v in vertices)
                    centroid += v;
                centroid /= vertices.Length;
                Vector3 size = mapping.FeatureMeshes[0].bounds.size;
                size.y = Mathf.Min(50, size.y);
                size.x = Mathf.Clamp(size.x, 50, 100);
                size.z = Mathf.Clamp(size.z, 50, 100);
                Bounds currentObjectBounds = new Bounds(mapping.SelectedGameObject.transform.position + centroid, size);
                return currentObjectBounds;
            }
            if (mapping.VisualisationLayer is GeoJSONPointLayer)
            {
                Bounds currentObjectBounds = new Bounds(mapping.SelectedGameObject.transform.position + mapping.FeatureMeshes[0].vertices[0] - mapping.FeatureMeshes[0].bounds.center, Vector3.one * 50);
                return currentObjectBounds;
            }

            return new();
        }

        private void ClearFeatureMappingsForLayer()
        {
            if(visualization == null)
                return;
            
            FeaturePropertyData propertyData = visualization.LayerData.GetProperty<FeaturePropertyData>();
            featureIds.Clear();
            propertyData.FeatureIds = null; //dont clear but set to null to trigger changed event
        }
    }
}