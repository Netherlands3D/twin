using Netherlands3D.CartesianTiles;
using Netherlands3D.LayerStyles;
using System.Collections.Generic;
using System.Linq;
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
                    LayerFeature feature = layer.CreateFeature(material); //one feature per default shared material

                    //populate the layerstyles based on available materials if not present in data yet
                    LayerStyle style = new LayerStyle(material.name);                   
                    StylingRule stylingRule = new StylingRule(material.name, $"[{Constants.MaterialNameIdentifier}=\"{material.name}\"]");
                    style.StylingRules.Add(material.name, stylingRule);
                    layer.LayerData.AddStyle(style);

                    //only load the symbolizer color when present in the data
                    var symbolizer = layer.LayerData.Styles[material.name].AnyFeature.Symbolizer;
                    //var symbolizer = layer.GetStyling(feature);
                    Color? color = symbolizer.GetFillColor();
                    if(color != null)
                        layer.UpdateMaterial((Color)color, index);
                    index++; 
                }                
            }

            public void SetIndices(List<int> materialIndices)
            {
                this.materialIndices = materialIndices;
                //create a styling rule to match the selected feature layers when applying styles
                string expression = string.Join(",\n", materialIndices.Select(index => $"[{Constants.MaterialIndexIdentifier}={index}]"));
                StylingRule matchIndexRule = new StylingRule("matchindexrule", expression);

                //we want to overwrite the default stylingrule!? otherwise will always return true without filtering                
                layer.LayerData.DefaultStyle.StylingRules.Clear(); 
                layer.LayerData.DefaultStyle.StylingRules.Add("default", matchIndexRule);
            }

            public Material CreateMaterial()
            {
                var defaultSymbolizer = layer.LayerData.DefaultStyle.AnyFeature.Symbolizer;
                //check if the color picker fill color is used
                if (defaultSymbolizer.GetFillColor() == null) 
                    return null;
                
                //colorpicker has a selected fill color and a possible selection of feature layers
                Color colorPickedColor = (Color)defaultSymbolizer.GetFillColor();
                foreach (int i in materialIndices)
                {
                    //var symbolizer = layer.GetStyling(layer.GetFeature(GetMaterialByIndex(i)));
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
