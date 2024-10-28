using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Netherlands3D.LayerStyles
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/styling", Name = "Symbolizer")]
    public class Symbolizer
    {
        [DataMember(Name = "properties")] 
        private Dictionary<string, object> properties = new();

        internal object GetProperty(string key)
        {
            // explicitly return null when value is not present, so that caller knows it should ignore using this field 
            return properties.ContainsKey(key) ? properties[key] : null;
        }

        internal void SetProperty(string key, object value)
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