using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Newtonsoft.Json;
using System.Runtime.Serialization;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Layer
{
    public class FirstPersonLayerPropertyData : TransformLayerPropertyData
    {
        [DataMember] public string movementName;

        [JsonIgnore] public string MovementName => movementName;


        [JsonConstructor]
        public FirstPersonLayerPropertyData(Coordinate position, Vector3 eulerRotation, Vector3 localScale) : base(position, eulerRotation, localScale)
        {
        }
    }
}
