using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

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

        [DataMember] protected List<string> customFlags { get; set; } = null;

        [JsonIgnore] public List<string> CustomFlags => customFlags;
               
        public void SetCustomFlags(List<string> flags)
        {
            customFlags = flags;
        }
    }
}
