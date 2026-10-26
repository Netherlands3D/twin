using System.Runtime.Serialization;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Folder")]
    [DataContractAliases(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Names = new[] { "Scenario" })]
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
