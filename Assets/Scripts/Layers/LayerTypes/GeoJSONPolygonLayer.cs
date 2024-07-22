using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [Serializable]
    public class GeoJSONPolygonLayer : LayerNL3DBase
    {
        public class FeatureSpawnedVisualisation
        {
            public Feature feature;
            public List<PolygonVisualisation> visualisations = new();
            public Bounds bounds;


            private float boundsRoundingCeiling = 1000;
            public float BoundsRoundingCeiling { get => boundsRoundingCeiling; set => boundsRoundingCeiling = value; }

            /// <summary>
            /// Calculate bounds by combining all visualisation bounds
            /// </summary>
            public void CalculateBounds()
            {
                if (visualisations.Count > 0)
                {
                    bounds = GetVisualisationBounds(visualisations[0]);

                    for(int i = 1; i < visualisations.Count; i++)
                        GetVisualisationBounds(visualisations[i]);
                }

                // Expand bounds to ceiling to steps
                bounds.size = new Vector3(
                    Mathf.Ceil(bounds.size.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Ceil(bounds.size.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Ceil(bounds.size.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
                );
                bounds.center = new Vector3(
                    Mathf.Round(bounds.center.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Round(bounds.center.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Round(bounds.center.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
                );
            }

            /// <summary>
            /// A nice addition to PolygonVisualisation script in package can be to add a method there to get the bounds of the visualisation (with cached MeshRenderer)
            /// </summary>
            /// <returns></returns>
            public static Bounds GetVisualisationBounds(PolygonVisualisation polygonVisualisation)
            {
                return polygonVisualisation.GetComponent<MeshRenderer>().bounds;
            }
        }

        public List<FeatureSpawnedVisualisation> SpawnedVisualisations = new();
        public List<PolygonVisualisation> PolygonVisualisations { get; private set; } = new();
        private bool randomizeColorPerFeature = false;
        public bool RandomizeColorPerFeature { get => randomizeColorPerFeature; set => randomizeColorPerFeature = value; }

        private Material polygonVisualizationMaterial;

        public Material PolygonVisualizationMaterial
        {
            get { return polygonVisualizationMaterial; }
            set
            {
                polygonVisualizationMaterial = value;
                foreach (var visualization in PolygonVisualisations)
                {
                    visualization.GetComponent<MeshRenderer>().material = polygonVisualizationMaterial;
                }
            }
        }

        public GeoJSONPolygonLayer(string name) : base(name)
        {
            ProjectData.Current.AddStandardLayer(this);
        }
        
        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            foreach (var visualization in PolygonVisualisations)
            {
                visualization.gameObject.SetActive(activeInHierarchy);
            }
        }

        public void AddAndVisualizeFeature<T>(Feature feature, CoordinateSystem originalCoordinateSystem)
            where T : GeoJSONObject
        {
            // Skip if feature already exists (comparison is done using hashcode based on geometry)
            if (SpawnedVisualisations.Any(f => f.feature.GetHashCode() == feature.GetHashCode()))
                return;

            // Create visual with random color if enabled
            var featureMaterial = PolygonVisualizationMaterial;
            if (RandomizeColorPerFeature){
                featureMaterial = new Material(PolygonVisualizationMaterial)
                {
                    color = UnityEngine.Random.ColorHSV()
                };
            }

            var newFeatureVisualisation = new FeatureSpawnedVisualisation { feature = feature };
            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                var polygonVisualisations = GeoJSONGeometryVisualizerUtility.VisualizeMultiPolygon(multiPolygon, originalCoordinateSystem, featureMaterial);
                newFeatureVisualisation.visualisations = polygonVisualisations;
            }
            else if(feature.Geometry is Polygon polygon)
            {
                var singlePolygonVisualisation = GeoJSONGeometryVisualizerUtility.VisualizePolygon(polygon, originalCoordinateSystem, featureMaterial);
                newFeatureVisualisation.visualisations.Append(singlePolygonVisualisation);
            }
            
            SpawnedVisualisations.Add(newFeatureVisualisation);
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (Application.isPlaying)
            {
                // Remove all SpawnedVisualisations
                foreach (var featureVisualisation in SpawnedVisualisations)
                {
                    RemoveFeature(featureVisualisation);
                }
            }
        }

        /// <summary>
        /// Checks the Bounds of the visualisations and checks them against the camera frustum
        /// to remove visualisations that are out of view
        /// </summary>
        public void RemoveFeaturesOutOfView()
        {         
            // Remove visualisations that are out of view
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            for (int i = SpawnedVisualisations.Count - 1; i >= 0 ; i--)
            {
                // Make sure to recalculate bounds because they can change due to shifts
                SpawnedVisualisations[i].CalculateBounds();

                var inCameraFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, SpawnedVisualisations[i].bounds);
                if (inCameraFrustum)
                    continue;

                var featureVisualisation = SpawnedVisualisations[i];
                RemoveFeature(featureVisualisation);
            }
        }
        
        private void RemoveFeature(FeatureSpawnedVisualisation featureVisualisation)
        {
            foreach (var polygonVisualisation in featureVisualisation.visualisations)
            {
                PolygonVisualisations.Remove(polygonVisualisation);
                if(polygonVisualisation.gameObject)
                    GameObject.Destroy(polygonVisualisation.gameObject);
            }
            SpawnedVisualisations.Remove(featureVisualisation);
        }
    }
}