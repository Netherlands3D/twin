using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    [Serializable]
    public class LayerPropertyData
    {
        /// <summary>
        /// Property data has a unique identifier for tracking which data belongs to this
        /// property; such as assets. 
        /// </summary>
        [DataMember] public Guid UUID = Guid.NewGuid();
    }
}
