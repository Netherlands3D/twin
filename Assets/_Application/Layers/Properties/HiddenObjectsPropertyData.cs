using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SerializableGisExpressions;
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
        
        [JsonIgnore] private Material selectionMaterial;
        
        [JsonIgnore] public readonly UnityEvent<string> OnHiddenObjectDataStylingRuleRemoved = new();
        
        [JsonIgnore]
        public Material SelectionMaterial
        {
            get => selectionMaterial;
            set => selectionMaterial = value;
        }

        public struct SubObjectData
        {
            public LayerFeature layerFeature;
            public string id;
            public bool visible;
            public Coordinate coord;
        }
     
        public void SetVisibilityForSubObject(LayerFeature layerFeature, bool visible, Coordinate coordinate, bool notify = true)
        {
            string id = layerFeature.Attributes[VisibilityAttributeIdentifier];
            SetVisibilityForSubObjectById(id, visible, coordinate, notify);
        }   
        
        public void SetVisibilityForSubObjectById(string objectId, bool visible, Coordinate coordinate, bool notify = true)
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

        private Dictionary<string, StylingRule>  stylingRuleKeys = new();
        public void SetVisibilityForSubObjects(List<SubObjectData> objects)
        {
            stylingRuleKeys.Clear();
            foreach (SubObjectData obj in objects)
            {
                var stylingRuleName = obj.layerFeature != null ? obj.layerFeature.Attributes[VisibilityAttributeIdentifier] : obj.id;
                var stylingRuleKey = VisibilityStyleRuleKey(stylingRuleName);

                // Add or set the colorization of this feature by its material index
                var stylingRule = new StylingRule(
                    stylingRuleName,
                    Expression.EqualTo(
                        Expression.Get(VisibilityAttributeIdentifier),
                        obj.layerFeature != null ? obj.layerFeature.Attributes[VisibilityAttributeIdentifier] : obj.id
                    )
                );
                stylingRule.Symbolizer.SetVisibility(obj.visible);
                stylingRule.Symbolizer.SetCustomProperty(VisibilityAttributePositionIdentifier, obj.coord);
                stylingRuleKeys.Add(stylingRuleKey, stylingRule);
            }
            
            SetStylingRules(stylingRuleKeys);
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
            RemoveStylingRule(stylingRuleKey);
            OnHiddenObjectDataStylingRuleRemoved.Invoke(id);
        }
        
        private string VisibilityStyleRuleKey(string visibilityIdentifier)
        {
            return $"feature.{visibilityIdentifier}.{VisibilityIdentifier}";
        }
        
        public HiddenObjectsPropertyData(Material selectionMaterial)
        {
            this.selectionMaterial = selectionMaterial;
        }
        
        [JsonConstructor]
        public HiddenObjectsPropertyData()
        {
            
        }
    }
}
