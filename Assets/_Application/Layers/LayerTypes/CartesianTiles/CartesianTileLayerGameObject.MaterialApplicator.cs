using Netherlands3D.CartesianTiles;
using Netherlands3D.LayerStyles;
using NUnit.Framework;
using System.Collections.Generic;
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
                BinaryMeshLayer binaryMeshLayer = layer.layer as BinaryMeshLayer;
                List<Material> materials = binaryMeshLayer.DefaultMaterialList;
                foreach (Material material in materials)
                {
                    layer.CreateFeature(material); //one feature per default shared material
                }
            }

            public void SetIndex(int materialIndex)
            {
                this.materialIndex = materialIndex;
            }

            public Material CreateMaterial()
            {
                //todo explain why materials are used here and not submeshes
                var style = layer.GetStyling(layer.GetFeature(GetMaterial())); //one feature per default shared material
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
