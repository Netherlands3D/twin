using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "Symbolizer")]
    public class Symbolizer
    {
        /// <summary>
        /// Store each property as a string, and trust that the SymbologyExtensions (such as
        /// Netherlands3D.LayerStyles.VectorSymbologyExtension.SetFillColor) will convert from and to string. During
        /// testing it became clear that trying to use serialization with JsonConverters can backfire because some
        /// classes do not, or should not, store type information and deserializing will then fail and return null
        /// values.
        ///
        /// As such: we simply use `dictionary with string,string` and use getters and setters to transform properties.
        /// </summary>
        [DataMember(Name = "properties")] 
        private Dictionary<string, string> properties = new();

        internal object GetProperty(string key)
        {
            // explicitly return null when value is not present, so that caller knows it should ignore using this field 
            return properties.ContainsKey(key) ? properties[key] : null;
        }

        internal void SetProperty(string key, string value)
        {
            properties[key] = value;
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
    }
}