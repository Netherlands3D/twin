using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layer
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "FirstPerson")]
    public class FirstPersonLayerPropertyData : LayerPropertyData
    {
        [DataMember] private int movementID;

        [JsonIgnore] public int MovementID => movementID;


        [JsonConstructor]
        public FirstPersonLayerPropertyData()
        {
            
        }

        public void SetMovementID(int movementID) => this.movementID = movementID;
    }
}
