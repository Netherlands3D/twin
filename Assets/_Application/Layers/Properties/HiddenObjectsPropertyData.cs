using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "HiddenObjectsData")]
    public class HiddenObjectsPropertyData : StylingPropertyData
    {
        public const string VisibilityAttributeIdentifier = "data-visibility";
        public const string VisibilityAttributePositionIdentifier = "data-visibility-position";
        public const string VisibilityIdentifier = "visibility";
     
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
            
            SetStylingRule(stylingRuleName, stylingRule);
        }

        public bool? GetVisibilityForSubObject(LayerFeature layerFeature)
        {
            string id = layerFeature.GetAttribute(VisibilityAttributeIdentifier);
            return GetVisibilityForSubObjectByAttributeTag(id);
        }

        public bool? GetVisibilityForSubObjectByAttributeTag(string id)
        {
            var stylingRuleName = VisibilityStyleRuleName(id);

            if (!StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                return true;
            }

            return stylingRule.Symbolizer.GetVisibility();
        }

        public void RemoveVisibilityForSubObjectByAttributeTag(string id)
        {
            var stylingRuleName = VisibilityStyleRuleName(id);
            bool dataRemoved = StylingRules.Remove(stylingRuleName);
        }

        public Coordinate? GetVisibilityCoordinateForSubObject(LayerFeature layerFeature)
        {
            string id = layerFeature.GetAttribute(VisibilityAttributeIdentifier);
            return GetVisibilityCoordinateForSubObjectByTag(id);
        }

        public Coordinate? GetVisibilityCoordinateForSubObjectByTag(string objectId)
        {
            var stylingRuleName = VisibilityStyleRuleName(objectId);
            if (!StylingRules.TryGetValue(stylingRuleName, out var stylingRule))
            {
                return null;
            }
            return stylingRule.Symbolizer.GetCustomProperty<Coordinate>(VisibilityAttributePositionIdentifier);
        }
        
        private string VisibilityStyleRuleName(string visibilityIdentifier)
        {
            return $"feature.{visibilityIdentifier}.{VisibilityIdentifier}";
        }

        public string ObjectIdFromVisibilityStyleRuleName(string styleRuleName)
        {
            int startIndex = styleRuleName.IndexOf('.') + 1;
            int endIndex = styleRuleName.LastIndexOf('.');
            if (startIndex > 0 && endIndex > startIndex)
            {
                return styleRuleName.Substring(startIndex, endIndex - startIndex);
            }
            return null;
        }       
        
        [JsonConstructor]
        public HiddenObjectsPropertyData()
        {
            
        }
    }
}
