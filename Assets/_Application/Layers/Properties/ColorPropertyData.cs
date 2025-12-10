using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "FillColorData")]
    public class ColorPropertyData : StylingPropertyData
    {
        // [DataMember] private Color color;
        //
        // [JsonIgnore] public readonly UnityEvent<Color> OnColorChanged = new();
        //
        // [JsonIgnore]
        // public Color  Color
        // {
        //     get => color;
        //     set
        //     {
        //         color = value;
        //         OnColorChanged.Invoke(value);
        //     }
        // }

        [JsonConstructor]
        public ColorPropertyData()
        {
            
        }
    }
}
