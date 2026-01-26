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
        [DataMember] public Dictionary<string, object> settingValues = new Dictionary<string, object>(); //Used for storing value types not classes.

        [JsonIgnore] public int MovementID => movementID;

        public FirstPersonLayerPropertyData()
        {

        }

        public FirstPersonLayerPropertyData(int movementID, Dictionary<string, object> settingValues)
        {
            this.movementID = movementID;
            this.settingValues = settingValues;
        }

        public void SetMovementID(int movementID) => this.movementID = movementID;
    }
}
