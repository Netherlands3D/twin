using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "Symbolizer")]
    public sealed class Symbolizer
    {
        /// <summary>
        /// Store each property as a string, and use specific getters and setting to convert from and to string.
        ///
        /// During testing it became clear that trying to use serialization with JsonConverters can backfire because
        /// some classes do not, or should not, store type information and deserializing will then fail and return null
        /// values.
        ///
        /// As such: we simply use `dictionary with string,string` and use getters and setters to transform properties.
        /// </summary>
        [DataMember(Name = "properties")] private Dictionary<string, string> properties = new();

        #region Styles

        /// <link href="https://docs.mapbox.com/style-spec/reference/layers/#paint-fill-fill-color"/>
        public void SetFillColor(Color color) => SetAndNormalizeColor("fill-color", color);

        /// <link href="https://docs.mapbox.com/style-spec/reference/layers/#paint-fill-fill-color"/>
        public Color? GetFillColor() => GetAndNormalizeColor("fill-color");

        public void ClearFillColor() => ClearProperty("fill-color");

        public void SetMaskLayerMask(int maskLayerMask) => SetProperty("mask-layer-mask", Convert.ToString(maskLayerMask, 2));

        public int? GetMaskLayerMask()
        {
            var json = GetProperty("mask-layer-mask");
            if(json == null || string.IsNullOrEmpty((string)json))
                return null;
            
            var bitMaskString = (string)json;
            return StringToBitmask(bitMaskString);
        }

        private static int StringToBitmask(string bitString)
        {
            if (string.IsNullOrEmpty(bitString))
                throw new ArgumentException("Input string cannot be null or empty.", nameof(bitString));

            return Convert.ToInt32(bitString, 2);
        }
        
        public void ClearMaskLayerMask() => ClearProperty("mask-layer-mask");

        /// <link href="https://docs.mapbox.com/style-spec/reference/layers/#paint-line-line-color"/>
        /// <remarks>
        /// Originally, the implementation was based on OGC CartoSym, which uses the term "stroke-color"; because the
        /// mapbox implementation is easier to read, we refer to that now but for backwards-compatibility we still use
        /// the term Stroke Color instead of Mapbox' Line Color.
        /// </remarks>
        public void SetStrokeColor(Color color) => SetAndNormalizeColor("stroke-color", color);

        /// <link href="https://docs.mapbox.com/style-spec/reference/layers/#paint-line-line-color"/>
        /// <remarks>
        /// Originally, the implementation was based on OGC CartoSym, which uses the term "stroke-color"; because the
        /// mapbox implementation is easier to read, we refer to that now but for backwards-compatibility we still use
        /// the term Stroke Color instead of Mapbox' Line Color.
        /// </remarks>
        public Color? GetStrokeColor() => GetAndNormalizeColor("stroke-color");

        public void ClearStrokeColor() => ClearProperty("stroke-color");

        public void SetVisibility(bool visible) => SetProperty("visibility", visible ? "visible" : "none");

        public bool? GetVisibility()
        {         
            if (GetProperty("visibility") is not string property) return null;

            if (property == "visible") return true;

            return false;            
        }

        public void ClearVisibility() => ClearProperty("visibility");
        

        #endregion

        /// <summary>
        /// Populates the given Symbolizer where the values of otherSymbolizer are merged on top of the values of it.
        ///
        /// This method is used to 'cascade' the results of picking symbolizers from applicable StylingRules so that
        /// a LayerGameObject has a single and definitive Symbolizer to apply.
        ///
        /// Example: Suppose a GeoJSON Feature's attributes match 2 different styling rules, then the former one's
        /// values are combined with the latter one and the LayerGameObject can 'just' apply the values.
        ///
        /// This is designed to function in a similar way how CSS cascades, where the ordering of the cascading rules
        /// is left to the caller of this method. 
        /// </summary>
        public static Symbolizer Merge(Symbolizer symbolizer, Symbolizer otherSymbolizer)
        {
            foreach (var x in otherSymbolizer.properties)
            {
                symbolizer.properties[x.Key] = x.Value;
            }

            return symbolizer;
        }

        public override string ToString()
        {
            var result = "";
            foreach (var (name, value) in properties)
            {
                result += $"{name}: {value}\n";
            }

            return result;
        }

        #region Getting and setting properties, and normalisation of object types from/to string

        private void SetAndNormalizeColor(string propertyName, Color color)
        {
            SetProperty(propertyName, $"#{ColorUtility.ToHtmlStringRGBA(color)}");
        }

        private Color? GetAndNormalizeColor(string propertyName)
        {
            if (GetProperty(propertyName) is not string property) return null;

            // Previous versions of project files were missing a '#', this auto-corrects this 
            if (property.StartsWith('#') == false) property = "#" + property;

            if (!ColorUtility.TryParseHtmlString(property, out var color)) return null;

            return color;
        }

        private object GetProperty(string key)
        {
            // explicitly return null when value is not present, so that caller knows it should ignore using this field 
            return properties.ContainsKey(key) ? properties[key] : null;
        }

        private void SetProperty(string key, string value)
        {
            properties[key] = value;
        }

        /// <summary>
        /// When an override is no longer necessary, we should be able to clear it so that the it is no longer applied
        /// next time styling is applied.
        /// </summary>
        private void ClearProperty(string propertyName)
        {
            properties.Remove(propertyName);
        }

        public Nullable<T> GetValueOrNull<T>() where T : struct
        {
            // example: return null to indicate "no value"
            return null;
        }


        #endregion
    }
}