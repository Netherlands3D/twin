using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Folder")]
    public class FolderPropertyData : LayerPropertyData
    {
        [DataMember] private bool isScenario;
        [JsonIgnore] public UnityEvent<bool> IsScenarioChanged = new();

        public bool IsScenario
        {
            get => isScenario;
            set
            {
                isScenario = value; 
                Debug.Log("isscenario changed:" + isScenario);
                IsScenarioChanged?.Invoke(value);
            }
        }
        
        public FolderPropertyData(bool isScenario = false)
        {
            this.isScenario = isScenario;
        }
    }
}
