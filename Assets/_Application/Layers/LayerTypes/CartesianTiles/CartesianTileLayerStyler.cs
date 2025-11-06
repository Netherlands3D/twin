using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.SubObjects;
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
    public class CartesianTileLayerStyler : IStyler
    {
        public const string MaterialNameIdentifier = "data-materialname";
        public const string MaterialIndexIdentifier = "data-materialindex";
        public const string VisibilityAttributeIdentifier = "data-visibility";
        public const string VisibilityAttributePositionIdentifier = "data-visibility-position";
        public const string VisibilityIdentifier = "visibility";

        public static ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());

        private LayerGameObject layer;

        public CartesianTileLayerStyler(LayerGameObject layer)
        {
            this.layer = layer;
        }

        /// <summary>
        /// Sets a custom color for all layer features matching the material index of the given layer feature.
        /// </summary>
        public void SetColor(LayerFeature layerFeature, Color color)
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
            layer.LayerData.OnStylingApplied.Invoke();
        }

        /// <summary>
        /// Retrieves the color for any feature that matches the given feature's material index.
        ///
        /// This method will provide a color override that has been set earlier, or it will return the current
        /// material's color if none was set. This can help in the UI to set swatches.
        /// </summary>
        public Color? GetColor(LayerFeature layerFeature)
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

        public void SetVisibilityForSubObject(LayerFeature layerFeature, bool visible, Coordinate coordinate)
        {
            string id = layerFeature.Attributes[VisibilityAttributeIdentifier];
            SetVisibilityForSubObjectByAttributeTag(id, visible, coordinate);
        }   
        
        public void SetVisibilityForSubObjectByAttributeTag(string objectId, bool visible, Coordinate coordinate)
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
            

            layer.LayerData.DefaultStyle.StylingRules[stylingRuleName] = stylingRule;
            layer.LayerData.OnStylingApplied.Invoke();
        }

        public bool? GetVisibilityForSubObject(LayerFeature layerFeature)
        {
            string id = layerFeature.GetAttribute(VisibilityAttributeIdentifier);
            return GetVisibilityForSubObjectByAttributeTag(id);
        }

        public bool? GetVisibilityForSubObjectByAttributeTag(string id)
        {
            var stylingRuleName = VisibilityStyleRuleName(id);

            if (!layer.LayerData.DefaultStyle.StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                return true;
            }

            return stylingRule.Symbolizer.GetVisibility();
        }

        public void RemoveVisibilityForSubObjectByAttributeTag(string id)
        {
            var stylingRuleName = VisibilityStyleRuleName(id);
            bool dataRemoved = layer.LayerData.DefaultStyle.StylingRules.Remove(stylingRuleName);
        }

        public Coordinate? GetVisibilityCoordinateForSubObject(LayerFeature layerFeature)
        {
            string id = layerFeature.GetAttribute(VisibilityAttributeIdentifier);
            return GetVisibilityCoordinateForSubObjectByTag(id);
        }

        public Coordinate? GetVisibilityCoordinateForSubObjectByTag(string objectId)
        {
            var stylingRuleName = VisibilityStyleRuleName(objectId);
            if (!layer.LayerData.DefaultStyle.StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                return null;
            }
            return stylingRule.Symbolizer.GetCustomProperty<Coordinate>(VisibilityAttributePositionIdentifier);
        }

        /// <summary>
        /// The other methods deal with manipulating the styles for a layerfeature, this method takes the outcome of
        /// those actions and applies them to the materials for the binary mesh layer.
        /// </summary>
        public void Apply(Symbolizer styling, LayerFeature layerFeature)
        {
            ApplyMaterial(styling, layerFeature);
            ApplyVisibility(styling, layerFeature);
        }

        private void ApplyMaterial(Symbolizer styling, LayerFeature layerFeature)
        {
            if (layerFeature.Geometry is not Material material) return;

            Color? color = styling.GetFillColor();
            if (color.HasValue)
            {
                if (int.TryParse(layerFeature.Attributes[MaterialIndexIdentifier], out var materialIndex))
                {
                    BinaryMeshLayer binaryMeshLayer = (layer as CartesianTileLayerGameObject).Layer as BinaryMeshLayer;
                    binaryMeshLayer.DefaultMaterialList[materialIndex].color = color.Value;
                }
            }
        }

        private void ApplyVisibility(Symbolizer styling, LayerFeature layerFeature)
        {
            if (layerFeature.Geometry is not ObjectMappingItem item) return;

            bool? visiblity = styling.GetVisibility();
            if (visiblity.HasValue)
            {
                string id = layerFeature.Attributes[VisibilityAttributeIdentifier];
                var color = visiblity == true ? styling.GetFillColor() ?? Color.white : Color.clear;
                GeometryColorizer.InsertCustomColorSet(-2, new Dictionary<string, Color>() { { id, color } });
            }            
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