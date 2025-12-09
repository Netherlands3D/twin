using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    /// <summary>
    /// Helper class that helps with styling the CartesianTile layers.
    ///
    /// Cartesian Tile layers can be styled by changing the color for a material - or set of layer features. This class
    /// can provide helpers to ensure a consistent set of styling rules is made, and to manage them.  
    /// </summary>
    public class CartesianTileLayerStyler
    {
        public const string MaterialNameIdentifier = "data-materialname";
        public const string MaterialIndexIdentifier = "data-materialindex";
        public const string VisibilityAttributeIdentifier = "data-visibility";
        public const string VisibilityAttributePositionIdentifier = "data-visibility-position";
        public const string VisibilityIdentifier = "visibility";

        public const string LayerFeatureColoring = "LayerFeatureColoring";

        public static ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());


        public CartesianTileLayerStyler(LayerGameObject layer)
        {
           
        }

        /// <summary>
        /// Sets a custom color for all layer features matching the material index of the given layer feature.
        /// </summary>
        public static void SetColor(LayerFeature layerFeature, Color color, StylingPropertyData stylingPropertyData)
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

            stylingPropertyData.SetDefaultStylingRule(stylingRuleName, stylingRule);
        }

        /// <summary>
        /// Retrieves the color for any feature that matches the given feature's material index.
        ///
        /// This method will provide a color override that has been set earlier, or it will return the current
        /// material's color if none was set. This can help in the UI to set swatches.
        /// </summary>
        public static Color? GetColor(LayerFeature layerFeature, StylingPropertyData data)
        {
            if (layerFeature.Geometry is not Material mat) return null;

            int.TryParse(layerFeature.GetAttribute(MaterialIndexIdentifier), out int materialIndexIdentifier);
            var stylingRuleName = ColorizationStyleRuleName(materialIndexIdentifier);

            if (!data.DefaultStyle.StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                if(mat.HasProperty("_Color") || mat.HasProperty("_BaseColor")) //TODO check a list of standardized tags for color properties
                    return mat.color;
                else
                    return null;
            }
            return stylingRule.Symbolizer.GetFillColor();
        }

        public static void SetVisibilityForSubObject(LayerFeature layerFeature, bool visible, Coordinate coordinate, StylingPropertyData stylingPropertyData)
        {
            string id = layerFeature.Attributes[VisibilityAttributeIdentifier];
            SetVisibilityForSubObjectByAttributeTag(id, visible, coordinate, stylingPropertyData);
        }   
        
        public static void SetVisibilityForSubObjectByAttributeTag(string objectId, bool visible, Coordinate coordinate, StylingPropertyData stylingPropertyData)
        {
            var stylingRuleName = VisibilityStyleRuleName(objectId);

            // Add or set the colorization of this feature by its material index
            var stylingRule = new StylingRule(
                stylingRuleName,
                Expression.EqualTo(
                    Expression.Get(VisibilityAttributeIdentifier),
                    objectId
                )
            );
            stylingRule.Symbolizer.SetVisibility(visible);
            stylingRule.Symbolizer.SetCustomProperty(VisibilityAttributePositionIdentifier, coordinate);
            
            stylingPropertyData.SetDefaultStylingRule(stylingRuleName, stylingRule);
        }

        public static bool? GetVisibilityForSubObject(LayerFeature layerFeature, StylingPropertyData stylingPropertyData)
        {
            string id = layerFeature.GetAttribute(VisibilityAttributeIdentifier);
            return GetVisibilityForSubObjectByAttributeTag(id, stylingPropertyData);
        }

        public static bool? GetVisibilityForSubObjectByAttributeTag(string id, StylingPropertyData data)
        {
            var stylingRuleName = VisibilityStyleRuleName(id);

            if (!data.DefaultStyle.StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                return true;
            }

            return stylingRule.Symbolizer.GetVisibility();
        }

        public static void RemoveVisibilityForSubObjectByAttributeTag(string id, StylingPropertyData stylingPropertyData)
        {
            var stylingRuleName = VisibilityStyleRuleName(id);
            bool dataRemoved = stylingPropertyData.DefaultStyle.StylingRules.Remove(stylingRuleName);
        }

        public static Coordinate? GetVisibilityCoordinateForSubObject(LayerFeature layerFeature, StylingPropertyData stylingPropertyData)
        {
            string id = layerFeature.GetAttribute(VisibilityAttributeIdentifier);
            return GetVisibilityCoordinateForSubObjectByTag(id, stylingPropertyData);
        }

        public static Coordinate? GetVisibilityCoordinateForSubObjectByTag(string objectId, StylingPropertyData stylingPropertyData)
        {
            var stylingRuleName = VisibilityStyleRuleName(objectId);
            if (!stylingPropertyData.DefaultStyle.StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                return null;
            }
            return stylingRule.Symbolizer.GetCustomProperty<Coordinate>(VisibilityAttributePositionIdentifier);
        }
       
        private static string ColorizationStyleRuleName(int materialIndexIdentifier)
        {
            return $"feature.{materialIndexIdentifier}.colorize";
        }        

        private static string VisibilityStyleRuleName(string visibilityIdentifier)
        {
            return $"feature.{visibilityIdentifier}.{VisibilityIdentifier}";
        }

        public static string ObjectIdFromVisibilityStyleRuleName(string styleRuleName)
        {
            int startIndex = styleRuleName.IndexOf('.') + 1;
            int endIndex = styleRuleName.LastIndexOf('.');
            if (startIndex > 0 && endIndex > startIndex)
            {
                return styleRuleName.Substring(startIndex, endIndex - startIndex);
            }
            return null;
        }       
    }
}