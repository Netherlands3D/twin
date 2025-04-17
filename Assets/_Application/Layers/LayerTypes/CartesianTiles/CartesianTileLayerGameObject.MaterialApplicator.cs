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
            public List<int> MaterialIndices => materialIndices;
            private List<int> materialIndices = new List<int>();

            private readonly CartesianTileLayerGameObject layer;            

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

            public void SetIndices(List<int> materialIndices)
            {
                this.materialIndices = materialIndices;
            }

            public Material CreateMaterial()
            {
                foreach (int i in materialIndices)
                {
                    //todo explain why materials are used here and not submeshes
                    var style = layer.GetStyling(layer.GetFeature(GetMaterialByIndex(i))); //one feature per default shared material
                    var color = style.GetFillColor() ?? Color.white;

                    layer.UpdateMaterial(color, i);
                }
                return null;
            }

            public void SetMaterial(Material material)
            {
                 //materials are shared so nothing should be set
            }

            public Material GetMaterialByIndex(int index)
            {
                return layer.GetMaterialInstance(index);
            }

            public Material GetMaterial() => materialIndices.Count > 0 ? layer.GetMaterialInstance(materialIndices[0]) : null; //this is a sharedmaterial
        }
    }
}
