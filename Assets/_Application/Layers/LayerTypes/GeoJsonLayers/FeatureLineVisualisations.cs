using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public partial class GeoJSONLineLayer
    {
        public class FeatureLineVisualisations : IFeatureVisualisation<List<Coordinate>>
        {
            public List<List<Coordinate>> Data => lines;

            public Feature feature;
            private List<List<Coordinate>> lines = new();
            public Bounds tiledBounds;
            public Bounds trueBounds;
            private Vector3 boundsPadding;

            private float boundsRoundingCeiling = 1000;
            public float BoundsRoundingCeiling { get => boundsRoundingCeiling; set => boundsRoundingCeiling = value; }

            public FeatureLineVisualisations()
            {
                Origin.current.onPostShift.AddListener(OnOriginShifted);
            }

            ~FeatureLineVisualisations()
            {
                Origin.current.onPostShift.RemoveListener(OnOriginShifted);
            }

            private void OnOriginShifted(Coordinate from, Coordinate to)
            {
                CalculateBounds();
            }

            public void SetBoundsPadding(Vector3 padding)
            {
                boundsPadding = padding;
            }

            /// <summary>
            /// Calculate bounds by combining all visualisation bounds
            /// </summary>
            public void CalculateBounds()
            {
                // Create combined rounded bounds of all lines
                foreach (var line in lines)
                {
                    for (var i = 0; i < line.Count; i++)
                    {
                        var coordinate = line[i];
                        if (i == 0) 
                            trueBounds = new Bounds(coordinate.ToUnity(), Vector3.zero);
                        else
                            trueBounds.Encapsulate(coordinate.ToUnity());
                    }
                }
                trueBounds.Expand(boundsPadding);
                tiledBounds = new Bounds(trueBounds.center, trueBounds.size);
                
                // Expand bounds to ceiling to steps
                tiledBounds.size = new Vector3(
                    Mathf.Ceil(tiledBounds.size.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Ceil(tiledBounds.size.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Ceil(tiledBounds.size.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
                );
                tiledBounds.center = new Vector3(
                    Mathf.Round(tiledBounds.center.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Round(tiledBounds.center.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                    Mathf.Round(tiledBounds.center.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
                );
            }
        }
    }
}