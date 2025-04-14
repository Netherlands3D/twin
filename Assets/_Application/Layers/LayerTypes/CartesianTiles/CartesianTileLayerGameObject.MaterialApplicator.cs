using Netherlands3D.LayerStyles;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    public partial class CartesianTileLayerGameObject
    {
        internal class CartesianTileBinaryMeshLayerMaterialApplicator : IMaterialApplicatorAdapter
        {
            private readonly CartesianTileLayerGameObject layer;

            public int materialIndex = 0;

            public CartesianTileBinaryMeshLayerMaterialApplicator(CartesianTileLayerGameObject layer)
            {
                this.layer = layer;
            }

            public void SetIndex(int materialIndex)
            {
                this.materialIndex = materialIndex;
            }

            public Material CreateMaterial()
            {
                var style = layer.GetStyling(layer.CreateFeature(layer));
                var color = style.GetFillColor() ?? Color.white;

                return layer.UpdateMaterial(color, materialIndex);
            }

            public void SetMaterial(Material material)
            {
                 //materials are shared so nothing should be set
            }

            public Material GetMaterial() => layer.GetMaterialInstance(materialIndex); //this is a sharedmaterial
        }
    }
}
