using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GG.Extensions;
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
            public Vector3 padding;

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

            /// <summary>
            /// Calculate bounds by combining all visualisation bounds
            /// </summary>
            public void CalculateBounds()
            {
                // Create combined rounded bounds of all lines
                tiledBounds = new Bounds();
                trueBounds = new Bounds();
                foreach (var line in lines)
                {
                    foreach (var coordinate in line)
                    {
                        tiledBounds.Encapsulate(coordinate.ToUnity());
                    }
                }
                trueBounds.size = tiledBounds.size;
                trueBounds.Expand(padding);
                trueBounds.center = tiledBounds.center;

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