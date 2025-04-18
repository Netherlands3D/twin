using System;
using Netherlands3D.LayerStyles;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public partial class GeoJSONPolygonLayer
    {
        internal class GeoJsonPolygonLayerMaterialApplicator : IMaterialApplicatorAdapter
        {
            private readonly GeoJSONPolygonLayer layer;

            public GeoJsonPolygonLayerMaterialApplicator(GeoJSONPolygonLayer layer)
            {
                this.layer = layer;
            }

            public Material CreateMaterial()
            {
                // TODO: We now define the layer as one whole feature - but it should be divided into its actual
                // features based on the Feature table of the GeoJSON output; but we do not have a tracking mechanism
                // to determine which features belong to which materials. Without such a mechanism, you inadvertently
                // delete the materials of the 'other' features when you try to replace the material on one feature.
                // In a follow up issue we are introducing coloring per group of features, then we can actually work
                // around this.
                var style = layer.GetStyling(layer.CreateFeature(layer));
                var color = style.GetFillColor() ?? Color.white;

                return layer.GetMaterialInstance(color);
            }

            public void SetMaterial(Material material)
            {
                layer.polygonVisualizationMaterialInstance = material;
            }

            public Material GetMaterial() => layer.polygonVisualizationMaterialInstance;
        }
    }
}