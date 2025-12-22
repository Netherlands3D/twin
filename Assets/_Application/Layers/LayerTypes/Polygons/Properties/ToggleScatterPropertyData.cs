using System;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "ToggleScatter")]
    public class ToggleScatterPropertyData : LayerPropertyData
    {
        //For now this is mostly a placeholder class to be able to link the property section to the data, but this could be used to determine for example mesh simplification before scattering. 
        [DataMember] private bool isScattered;
        [DataMember] private bool allowScatter;
        
        [JsonIgnore] public readonly UnityEvent<bool> IsScatteredChanged = new();
        [JsonIgnore] public readonly UnityEvent<bool> AllowScatterChanged = new();
        
        [JsonIgnore]
        public bool IsScattered
        {
            get => isScattered;
            set
            {
                isScattered = value;
                IsScatteredChanged.Invoke(isScattered);
            }
        }

        [JsonIgnore]
        public bool AllowScatter
        {
            get => allowScatter;
            set
            {
                allowScatter = value;
                AllowScatterChanged.Invoke(allowScatter);
            }
        }
    }
}
