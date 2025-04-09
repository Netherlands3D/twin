using System.Linq;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Rendering;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public partial class GeoJSONLineLayer
    {
        internal class GeoJsonLineLayerMaterialApplicator : IMaterialApplicatorAdapter
        {
            private readonly GeoJSONLineLayer layer;

            public GeoJsonLineLayerMaterialApplicator(GeoJSONLineLayer layer)
            {
                this.layer = layer;
            }

            public Material CreateMaterial()
            {
                //var features = layer.GetFeatures<BatchedMeshInstanceRenderer>();
                //if (features.Count == 0)
                //    return layer.GetMaterialInstance(Color.white);
                //var style = layer.GetStyling(features.FirstOrDefault());
                var style = layer.GetStyling(layer.CreateFeature(layer.spawnedVisualisations.Keys.FirstOrDefault()));
                var color = style.GetFillColor() ?? Color.white;
                Material mat = layer.GetMaterialInstance(color);
                mat.SetColor("_Color", color);
                return mat;
            }

            public void SetMaterial(Material material) => layer.LineRenderer3D.LineMaterial = material;
            public Material GetMaterial() => layer.LineRenderer3D.LineMaterial;
        }
    }
}