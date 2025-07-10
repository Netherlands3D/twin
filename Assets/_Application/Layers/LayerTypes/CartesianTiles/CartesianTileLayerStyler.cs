using Netherlands3D.CartesianTiles;
using Netherlands3D.LayerStyles;
using Netherlands3D.LayerStyles.Expressions;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    /// <summary>
    /// Helper class that helps with styling the CartesianTile layers.
    ///
    /// Cartesian Tile layers can be styled by changing the color for a material - or set of layer features. This class
    /// can provide helpers to ensure a consistent set of styling rules is made, and to manage them.  
    /// </summary>
    public static class CartesianTileLayerStyler
    {
        public const string MaterialNameIdentifier = "data-materialname";
        public const string MaterialIndexIdentifier = "data-materialindex";

        /// <summary>
        /// Sets a custom color for all layer features matching the material index of the given layer feature.
        /// </summary>
        public static void SetColor(LayerGameObject layer, LayerFeature layerFeature, Color color)
        {
            int.TryParse(layerFeature.Attributes[MaterialIndexIdentifier], out int materialIndexIdentifier);

            var stylingRuleName = ColorizationStyleRuleName(materialIndexIdentifier);

            // Add or set the colorization of this feature by its material index
            var stylingRule = new StylingRule(
                stylingRuleName, 
                Expression.EqualTo(
                    Expression.Get(MaterialIndexIdentifier),
                    materialIndexIdentifier.ToString()
                )
            );
            stylingRule.Symbolizer.SetFillColor(color);
                
            layer.LayerData.DefaultStyle.StylingRules[stylingRuleName] = stylingRule;
            layer.ApplyStyling();
        }

        /// <summary>
        /// Retrieves the color for any feature that matches the given feature's material index.
        ///
        /// This method will provide a color override that has been set earlier, or it will return the current
        /// material's color if none was set. This can help in the UI to set swatches.
        /// </summary>
        public static Color? GetColor(LayerGameObject layer, LayerFeature layerFeature)
        {
            int.TryParse(layerFeature.GetAttribute(MaterialIndexIdentifier), out int materialIndexIdentifier);
            var stylingRuleName = ColorizationStyleRuleName(materialIndexIdentifier);
            
            var defaultColor = ((Material)layerFeature.Geometry).color;
            if (!layer.LayerData.DefaultStyle.StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                return defaultColor;
            }

            return stylingRule.Symbolizer.GetFillColor();
        }

        /// <summary>
        /// The other methods deal with manipulating the styles for a layerfeature, this method takes the outcome of
        /// those actions and applies them to the materials for the binary mesh layer.
        /// </summary>
        public static void Apply(BinaryMeshLayer layer, Symbolizer styling, LayerFeature layerFeature)
        {
            Color? color = styling.GetFillColor();
            if (!color.HasValue) return;

            if (!int.TryParse(layerFeature.Attributes[MaterialIndexIdentifier], out var materialIndex)) return;

            layer.DefaultMaterialList[materialIndex].color = color.Value;
        }

        private static string ColorizationStyleRuleName(int materialIndexIdentifier)
        {
            return $"feature.{materialIndexIdentifier}.colorize";
        }
    }
}