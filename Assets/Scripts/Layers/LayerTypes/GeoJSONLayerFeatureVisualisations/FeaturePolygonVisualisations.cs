using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.SelectionTools;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public partial class GeoJSONPolygonLayer
    {
        public class FeaturePolygonVisualisations
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

            public void DestroyAllVisualisations()
            {
                foreach (var visualisation in visualisations)
                {
                    if (visualisation != null)
                        Object.Destroy(visualisation.gameObject);
                }
                visualisations.Clear();
            }

            public void ShowVisualisations(bool show)
            {
                foreach (var visualisation in visualisations)
                {
                    visualisation.gameObject.SetActive(show);
                }
            }

            /// <summary>
            /// Set the material of all visualisations
            /// </summary>
            public void SetMaterial(Material material)
            {
                foreach (var visualisation in visualisations)
                {
                    visualisation.GetComponent<Renderer>().material = material;
                }
            }

            /// <summary>
            /// A nice addition/optimisation would be to cache this inside the PolygonVisualisation class (needs change in package)
            /// </summary>
            /// <returns>The bounds of the MeshRenderer</returns>
            private Bounds GetVisualisationBounds(PolygonVisualisation polygonVisualisation)
            {
                return polygonVisualisation.GetComponent<MeshRenderer>().bounds;
            }
        }
    }
}