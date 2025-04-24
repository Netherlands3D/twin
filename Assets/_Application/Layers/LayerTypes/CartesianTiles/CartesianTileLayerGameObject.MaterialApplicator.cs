using Netherlands3D.CartesianTiles;
using Netherlands3D.LayerStyles;
using Netherlands3D.LayerStyles.Expressions;
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
                int index = 0;
                foreach (Material material in materials)
                {                    
                    layer.CreateFeature(material); //one feature per default shared material

                    LayerStyle style = new LayerStyle(material.name);
                    StylingRule stylingRule = new StylingRule(material.name, new MatchMaterialNameExpression(material.name));
                    style.StylingRules.Add(material.name, stylingRule);
                    layer.LayerData.AddStyle(style);

                    var symbolizer = layer.LayerData.Styles[material.name].AnyFeature.Symbolizer;
                    Color? color = symbolizer.GetFillColor();
                    if(color != null)
                        layer.UpdateMaterial((Color)color, index);
                    index++; 
                }                
            }

            public void SetIndices(List<int> materialIndices)
            {
                this.materialIndices = materialIndices;
                MatchMaterialIndexExpression matchIndex = new MatchMaterialIndexExpression(materialIndices);
                StylingRule matchIndexRule = new StylingRule("matchindexrule", matchIndex);

                //we want to overwrite the default stylingrule!?                
                layer.LayerData.DefaultStyle.StylingRules.Clear(); 
                layer.LayerData.DefaultStyle.StylingRules.Add("default", matchIndexRule);
            }

            public Material CreateMaterial()
            {
                var defaultSymbolizer = layer.LayerData.DefaultStyle.AnyFeature.Symbolizer;
                if (defaultSymbolizer.GetFillColor() == null) //the color picker fill color is not used yet so lets do nothing here
                {
                    return null;
                }   
                Color colorPickedColor = (Color)defaultSymbolizer.GetFillColor();
                foreach (int i in materialIndices)
                {                    
                    var symbolizer = layer.LayerData.Styles[GetMaterialByIndex(i).name].AnyFeature.Symbolizer;
                    symbolizer.SetFillColor(colorPickedColor);
                    layer.UpdateMaterial(colorPickedColor, i);
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
