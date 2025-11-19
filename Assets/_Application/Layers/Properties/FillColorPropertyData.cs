using Netherlands3D.Coordinates;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "FillColor")]
    public class FillColorPropertyData : LayerPropertyData
    {
        [DataMember] private Color color;

        [JsonIgnore]
        public Color Color
        {
            get => color;
            set
            {                
                color = value;
                OnColorChanged.Invoke(color);
            }
        }

        [JsonIgnore] public readonly UnityEvent<Color> OnColorChanged = new();

        public FillColorPropertyData()
        {
            color = Color.white;
        }

        [JsonConstructor]
        public FillColorPropertyData(Color color)
        {
            this.color = color;
        }
    }
}