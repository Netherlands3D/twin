using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.SubObjects;
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
        public const string VisibilityIdentifier = "data-visibility";
        public const string VisibilityPositionIdentifier = "data-visibility-position";

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
            layer.ApplyStyling();
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

        public void SetVisibilityForSubObject(LayerFeature layerFeature, bool visible)
        {
            string id = layerFeature.Attributes[VisibilityIdentifier];

            var stylingRuleName = VisibilityStyleRuleName(id);

            // Add or set the colorization of this feature by its material index
            var stylingRule = new StylingRule(
                stylingRuleName,
                Expression.EqualTo(
                    Expression.Get(VisibilityIdentifier),
                    id
                )
            );
            stylingRule.Symbolizer.SetVisibility(visible);

            layer.LayerData.DefaultStyle.StylingRules[stylingRuleName] = stylingRule;
            layer.ApplyStyling();
        }        

        public bool? GetVisibilityForSubObject(LayerFeature layerFeature)
        {
            string id = layerFeature.GetAttribute(VisibilityIdentifier);

            var stylingRuleName = VisibilityStyleRuleName(id);

            if (!layer.LayerData.DefaultStyle.StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                return true;
            }

            return stylingRule.Symbolizer.GetVisibility();
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
                string id = layerFeature.Attributes[VisibilityIdentifier];
                if (visiblity == true)
                {
                    var color = styling.GetFillColor() ?? Color.white;
                    GeometryColorizer.InsertCustomColorSet(-2, new Dictionary<string, Color>() { { id, color } });

                    //TODO we need a check or callback when the panel will close and all visible layerfeatures are removed
                    //layerFeature.Attributes.Remove(VisibilityPositionIdentifier);

                }
                else
                {
                    var color = Color.clear;
                    GeometryColorizer.InsertCustomColorSet(-2, new Dictionary<string, Color>() { { id, color } });

                    if(layerFeature.Attributes.ContainsKey(VisibilityPositionIdentifier)) return;

                    CartesianTileLayerGameObject cartesianTileLayerGameObject = layer as CartesianTileLayerGameObject;
                    ObjectMapping objectMapping = cartesianTileLayerGameObject.FindObjectMapping(item);
                    Coordinate coord = cartesianTileLayerGameObject.GetCoordinateForObjectMappingItem(objectMapping, item);
                    layerFeature.Attributes.Add(VisibilityPositionIdentifier, VisibilityPositionIdentifierValue(item.objectID, coord));
                }
            }            
        }
       
        private static string ColorizationStyleRuleName(int materialIndexIdentifier)
        {
            return $"feature.{materialIndexIdentifier}.colorize";
        }        

        private static string VisibilityStyleRuleName(string visibilityIdentifier)
        {
            return $"feature.{visibilityIdentifier}.visibility";
        }

        public static string VisibilityPositionIdentifierValue(string id, Coordinate position)
        {
            return id + position.ToString();
        }

        public static Coordinate VisibilityPositionFromIdentifierValue(string value)
        {
            int index = value.IndexOf('(');
            if (index > 0)
            {
                string coordString = value.Substring(index);
                coordString = coordString.Trim('(', ')');
                var parts = coordString.Split(',');

                double value1 = double.Parse(parts[0]);
                double value2 = double.Parse(parts[1]);
                double value3 = double.Parse(parts[2]);
                Coordinate coord = new Coordinate(CoordinateSystems.connectedCoordinateSystem, value1, value2, value3);
                return coord;
            }
            return default;
        }
    }
}