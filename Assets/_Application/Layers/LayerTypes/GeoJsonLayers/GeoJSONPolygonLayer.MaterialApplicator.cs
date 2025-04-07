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
            private FeaturePolygonVisualisations visualisation;

            public GeoJsonPolygonLayerMaterialApplicator(GeoJSONPolygonLayer layer)
            {
                this.layer = layer;
            }

            public void ApplyTo(FeaturePolygonVisualisations visualisation)
            {
                this.visualisation = visualisation;
            }

            public Material CreateMaterial()
            {
                if (visualisation == null)
                {
                    throw new System.ArgumentNullException("visualisation");
                }

                var style = layer.GetStyling(layer.CreateFeature(visualisation));
                var color = style.GetFillColor() ?? Color.white;

                return layer.GetMaterialInstance(color);
            }

            public void SetMaterial(Material material)
            {
                if (visualisation == null)
                {
                    throw new ArgumentNullException("visualisation");
                }

                visualisation.SetMaterial(material);
            }

            public Material GetMaterial()
            {
                if (visualisation == null)
                {
                    throw new ArgumentNullException("visualisation");
                }

                return layer.polygonVisualizationMaterialInstance;
            }
        }
    }
}