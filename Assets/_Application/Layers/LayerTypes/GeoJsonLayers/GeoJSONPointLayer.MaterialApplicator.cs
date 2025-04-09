using System.Linq;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Rendering;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public partial class GeoJSONPointLayer
    {
        internal class GeoJsonPointLayerMaterialApplicator : IMaterialApplicatorAdapter
        {
            private readonly GeoJSONPointLayer layer;

            public GeoJsonPointLayerMaterialApplicator(GeoJSONPointLayer layer)
            {
                this.layer = layer;
            }

            public Material CreateMaterial()
            {
                var features = layer.GetFeatures<BatchedMeshInstanceRenderer>();
                if (features.Count == 0)
                    return layer.GetMaterialInstance(Color.white);

                var style = layer.GetStyling(features.FirstOrDefault());
                var color = style.GetFillColor() ?? Color.white;

                return layer.GetMaterialInstance(color);
            }

            public void SetMaterial(Material material) => layer.PointRenderer3D.Material = material;
            public Material GetMaterial() => layer.PointRenderer3D.Material;
        }
    }
}