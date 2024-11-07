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
    }
}