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
            SetVisibilityForSubObjectById(id, visible, coordinate);
        }   
        
        public void SetVisibilityForSubObjectById(string objectId, bool visible, Coordinate coordinate)
        {
            var stylingRuleName = objectId;
            var stylingRuleKey = VisibilityStyleRuleKey(objectId);

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
            
            SetStylingRule(stylingRuleKey, stylingRule);
        }

        public bool? GetVisibilityForSubObject(LayerFeature layerFeature)
        {
            string id = layerFeature.GetAttribute(VisibilityAttributeIdentifier);
            return GetVisibilityForSubObjectById(id);
        }

        public bool? GetVisibilityForSubObjectById(string id)
        {
            var stylingRuleKey = VisibilityStyleRuleKey(id);
            if (!StylingRules.TryGetValue(stylingRuleKey, out var stylingRule))
            {
                return true;
            }
            return stylingRule.Symbolizer.GetVisibility();
        }

        public Coordinate? GetVisibilityCoordinateForSubObject(LayerFeature layerFeature)
        {
            string id = layerFeature.GetAttribute(VisibilityAttributeIdentifier);
            return GetVisibilityCoordinateForSubObjectById(id);
        }

        public Coordinate? GetVisibilityCoordinateForSubObjectById(string id)
        {
            var stylingRuleKey = VisibilityStyleRuleKey(id);
            if (!StylingRules.TryGetValue(stylingRuleKey, out var stylingRule))
            {
                return null;
            }
            return stylingRule.Symbolizer.GetCustomProperty<Coordinate>(VisibilityAttributePositionIdentifier);
        }
        
        public void RemoveVisibilityForSubObjectById(string id)
        {
            var stylingRuleKey = VisibilityStyleRuleKey(id);
            bool dataRemoved = StylingRules.Remove(stylingRuleKey);
        }
        
        private string VisibilityStyleRuleKey(string visibilityIdentifier)
        {
            return $"feature.{visibilityIdentifier}.{VisibilityIdentifier}";
        }
        
        [JsonConstructor]
        public HiddenObjectsPropertyData()
        {
            
        }
    }
}
