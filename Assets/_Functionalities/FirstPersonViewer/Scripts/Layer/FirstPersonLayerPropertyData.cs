using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layers
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "FirstPerson")]
    public class FirstPersonLayerPropertyData : LayerPropertyData
    {
        [DataMember] private int movementID;
        [DataMember] public Dictionary<string, object> settingValues; 

        [JsonIgnore] public int MovementID => movementID;

        [JsonConstructor]
        public FirstPersonLayerPropertyData()
        {
            settingValues = new Dictionary<string, object>();
        }

        public FirstPersonLayerPropertyData(int movementID, Dictionary<string, object> settingValues)
        {
            this.movementID = movementID;
            this.settingValues = settingValues;
        }

        public void SetMovementID(int movementID) => this.movementID = movementID;
    }
}
